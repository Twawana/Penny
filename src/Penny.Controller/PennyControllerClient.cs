using Penny.Network;

namespace Penny.Controller;

public sealed class PennyControllerClient
{
    public async Task<ControllerSession> ConnectAsync(string host, int port, CancellationToken ct)
    {
        var transport = await TlsTcpTransport.ConnectAsClientAsync(host, port, ct).ConfigureAwait(false);
        return new ControllerSession(transport);
    }
}

public sealed class ControllerSession : IAsyncDisposable
{
    private readonly IPennyTransport _transport;
    private ulong _sequence;

    public ControllerSession(IPennyTransport transport)
    {
        _transport = transport;
        SessionId = Guid.NewGuid();
    }

    public Guid SessionId { get; }

    public Task SendPingAsync(CancellationToken ct) =>
        PennySessionLoop.SendPingAsync(_transport, _sequence++, ct);

    public Task EndSessionAsync(string reason, CancellationToken ct) =>
        PennySessionLoop.SendSessionEndAsync(_transport, _sequence++, reason, ct);

    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
