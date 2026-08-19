using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Penny.Network;

/// <summary>
/// Listens for inbound Controller connections on the Agent's LAN endpoint
/// (Stage 1 networking — see architecture notes). Each accepted socket is
/// TLS-authenticated and handed to <see cref="OnConnectionAsync"/> for the
/// caller (Penny.Agent) to run the auth/authorization handshake and, if
/// approved, the session loop.
///
/// This class deliberately knows nothing about PINs, approval prompts, or
/// screen capture — it only terminates TCP+TLS and hands off a transport.
/// Keeping it this thin makes it independently unit-testable and makes the
/// future Stage 2 signaling-server transport a drop-in alternative behind
/// the same IPennyTransport handoff.
/// </summary>
public sealed class PennyAgentListener : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly X509Certificate2 _certificate;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public int Port { get; }

    public PennyAgentListener(int port, X509Certificate2 certificate)
    {
        Port = port;
        _certificate = certificate;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    /// <summary>Invoked for every successfully TLS-authenticated inbound connection.</summary>
    public Func<IPennyTransport, CancellationToken, Task>? OnConnectionAsync { get; set; }

    /// <summary>Invoked when a raw connection fails TLS/TCP setup, for logging (never contains secrets).</summary>
    public Action<string, Exception>? OnConnectionSetupFailed { get; set; }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = HandleClientAsync(client, ct);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        string remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        try
        {
            var transport = await TlsTcpTransport.AuthenticateAsServerAsync(client, _certificate, ct)
                .ConfigureAwait(false);
            if (OnConnectionAsync is not null)
            {
                await OnConnectionAsync(transport, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            OnConnectionSetupFailed?.Invoke(remote, ex);
            client.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _listener.Stop();
        if (_acceptLoop is not null)
        {
            try { await _acceptLoop.ConfigureAwait(false); } catch { /* expected on cancel */ }
        }
        _cts?.Dispose();
    }
}
