using System.Net.Security;
using System.Net.Sockets;
using Penny.Protocol;

namespace Penny.Network;

/// <summary>
/// IPennyTransport implementation over a TcpClient + SslStream. Used by both
/// the Agent (server side, cert-authenticated) and the Controller (client side).
/// One instance per connection; not shared across threads for writes without
/// external synchronization (the caller — SessionAuthenticator / relay loops —
/// owns a single reader loop and serializes writes).
/// </summary>
public sealed class TlsTcpTransport : IPennyTransport
{
    private readonly TcpClient _tcpClient;
    private readonly SslStream _sslStream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private TlsTcpTransport(TcpClient tcpClient, SslStream sslStream)
    {
        _tcpClient = tcpClient;
        _sslStream = sslStream;
    }

    public string RemoteEndpoint => _tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";

    /// <summary>Agent side: wraps an already-accepted TcpClient in TLS server auth using the Agent's cert.</summary>
    public static async Task<TlsTcpTransport> AuthenticateAsServerAsync(
        TcpClient tcpClient,
        System.Security.Cryptography.X509Certificates.X509Certificate2 serverCertificate,
        CancellationToken ct)
    {
        var sslStream = new SslStream(tcpClient.GetStream(), leaveInnerStreamOpen: false);
        await sslStream.AuthenticateAsServerAsync(
            new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = false,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                      System.Security.Authentication.SslProtocols.Tls13,
            }, ct).ConfigureAwait(false);

        return new TlsTcpTransport(tcpClient, sslStream);
    }

    /// <summary>
    /// Controller side: connects and authenticates TLS. Penny does not validate the
    /// Agent's certificate against a public CA (Agents use self-signed certs — see
    /// AgentCertificateProvider), so we accept any server certificate here and rely
    /// on DeviceId + PIN + explicit Agent-side approval for authorization instead.
    /// TLS's role in this design is confidentiality/integrity of the pipe, not peer
    /// identity. A future hardening pass can pin the Agent's certificate thumbprint
    /// per DeviceId on first connect (trust-on-first-use) to also detect MITM on
    /// reconnect — tracked as a Phase 10 (security hardening) item.
    /// </summary>
    public static async Task<TlsTcpTransport> ConnectAsClientAsync(string host, int port, CancellationToken ct)
    {
        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port, ct).ConfigureAwait(false);

        var sslStream = new SslStream(
            tcpClient.GetStream(),
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: (_, _, _, _) => true); // see remarks above

        await sslStream.AuthenticateAsClientAsync(
            new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 |
                                      System.Security.Authentication.SslProtocols.Tls13,
            }, ct).ConfigureAwait(false);

        return new TlsTcpTransport(tcpClient, sslStream);
    }

    public async Task SendEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await FrameCodec.WriteEnvelopeAsync(_sslStream, envelope, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SendBinaryFrameAsync(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await FrameCodec.WriteBinaryFrameAsync(_sslStream, data, ct).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public Task<(byte Kind, byte[] Payload)> ReceiveFrameAsync(CancellationToken ct) =>
        FrameCodec.ReadFrameAsync(_sslStream, ct);

    public async ValueTask DisposeAsync()
    {
        _writeLock.Dispose();
        await _sslStream.DisposeAsync().ConfigureAwait(false);
        _tcpClient.Dispose();
    }
}
