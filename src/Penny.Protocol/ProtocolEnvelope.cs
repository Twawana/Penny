namespace Penny.Protocol;

/// <summary>
/// Wire envelope for every message on the control channel. Control messages
/// (Hello, Auth*, Mouse/KeyboardEvent, Ping/Pong, etc.) are JSON-encoded inside
/// <see cref="PayloadJson"/>. ScreenFrame is the one exception: its bytes travel
/// as a separate binary frame on the same stream (see BinaryFrameCodec) because
/// re-encoding compressed JPEG/H.264 bytes as JSON (base64) would waste ~33%
/// of bandwidth on a path that is already the system's bandwidth bottleneck.
/// </summary>
public sealed record ProtocolEnvelope
{
    /// <summary>Protocol version this message was produced with, e.g. "1.0".</summary>
    public required string Version { get; init; }

    public required MessageType Type { get; init; }

    /// <summary>Monotonically increasing per-connection sequence number, for ordering/dedup.</summary>
    public required ulong Sequence { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Correlates a request with its response (e.g. AuthRequest/AuthResponse).</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>JSON-encoded payload matching one of the record types in Penny.Protocol.Messages.</summary>
    public required string PayloadJson { get; init; }
}
