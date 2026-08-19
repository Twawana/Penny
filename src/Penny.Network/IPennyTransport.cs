using Penny.Protocol;

namespace Penny.Network;

/// <summary>
/// A single logical Penny connection: one TLS-wrapped TCP stream carrying
/// framed control envelopes and binary screen-frame payloads (see FrameCodec).
/// Both PennyAgentListener and PennyControllerClient hand out instances of this.
/// </summary>
public interface IPennyTransport : IAsyncDisposable
{
    /// <summary>Remote endpoint, e.g. "192.168.1.42:5000" — used for logging and rate limiting, never for auth.</summary>
    string RemoteEndpoint { get; }

    Task SendEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken ct);
    Task SendBinaryFrameAsync(ReadOnlyMemory<byte> data, CancellationToken ct);

    /// <summary>Reads the next frame off the wire. The caller inspects Kind to know how to interpret Payload.</summary>
    Task<(byte Kind, byte[] Payload)> ReceiveFrameAsync(CancellationToken ct);
}
