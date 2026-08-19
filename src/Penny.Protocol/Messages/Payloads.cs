namespace Penny.Protocol.Messages;

/// <summary>First message on any connection — capability/version negotiation.</summary>
public sealed record HelloPayload
{
    public required string ProtocolVersion { get; init; }
    public required string ClientKind { get; init; } // "Controller" | "Agent"
    public required string ClientDisplayName { get; init; }
}

/// <summary>Controller -> Agent: "I want to connect to this DeviceId with this PIN."</summary>
public sealed record AuthRequestPayload
{
    public required string TargetDeviceId { get; init; }
    public required string Pin { get; init; }
    public required string ControllerDisplayName { get; init; }
}

/// <summary>Agent -> Controller: PIN was structurally valid; approval is now pending the user.</summary>
public sealed record AuthResponsePayload
{
    public required bool PinAccepted { get; init; }
    public string? FailureReason { get; init; } // e.g. "InvalidPin", "Expired", "RateLimited"
}

/// <summary>Agent -> Controller (and Agent UI): final accept/reject decision from the local user.</summary>
public sealed record ConnectionDecisionPayload
{
    public required bool Approved { get; init; }
    public string? Reason { get; init; } // e.g. "RejectedByUser", "Timeout"
}

/// <summary>Either side -> other: session is now live; carries the session token to use going forward.</summary>
public sealed record SessionStartPayload
{
    public required Guid SessionId { get; init; }
    public required string SessionToken { get; init; }
    public required int ScreenWidth { get; init; }
    public required int ScreenHeight { get; init; }
}

/// <summary>Metadata for a ScreenFrame; the pixel bytes themselves follow as a binary frame (see BinaryFrameCodec).</summary>
public sealed record ScreenFrameMetaPayload
{
    public required uint FrameIndex { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string Codec { get; init; } // "jpeg" for MVP; "h264" once integrated
    public required int ByteLength { get; init; }
    public required bool IsKeyFrame { get; init; }
}

public enum MouseEventKind { Move, Down, Up, Wheel }
public enum MouseButton { None, Left, Right, Middle }

/// <summary>Normalized (0.0-1.0) coordinates so events are independent of either side's screen resolution.</summary>
public sealed record MouseEventPayload
{
    public required MouseEventKind Kind { get; init; }
    public required double NormalizedX { get; init; }
    public required double NormalizedY { get; init; }
    public MouseButton Button { get; init; } = MouseButton.None;
    public int WheelDelta { get; init; }
}

public enum KeyEventKind { Down, Up }

/// <summary>Virtual-key based, not raw scan codes, to keep layout handling on the Controller side explicit.</summary>
public sealed record KeyboardEventPayload
{
    public required KeyEventKind Kind { get; init; }
    public required int VirtualKeyCode { get; init; }
    public bool Shift { get; init; }
    public bool Control { get; init; }
    public bool Alt { get; init; }
}

public sealed record PingPayload
{
    public required DateTimeOffset SentAtUtc { get; init; }
}

public sealed record PongPayload
{
    public required DateTimeOffset EchoedSentAtUtc { get; init; }
    public required DateTimeOffset RepliedAtUtc { get; init; }
}

public sealed record SessionEndPayload
{
    public required string Reason { get; init; } // matches Penny.Core.Models.SessionEndReason.ToString()
}

public sealed record ErrorPayload
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
