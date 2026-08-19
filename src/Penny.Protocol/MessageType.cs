namespace Penny.Protocol;

/// <summary>
/// Every message type Penny's control channel can carry. Numeric values are
/// part of the wire format — append new values at the end, never renumber,
/// so ProtocolVersion mismatches degrade gracefully instead of misparsing.
/// </summary>
public enum MessageType : byte
{
    Hello = 0,
    AuthRequest = 1,
    AuthResponse = 2,
    ConnectionRequest = 3,   // Controller -> Agent: "please prompt the user to approve me"
    ConnectionDecision = 4,  // Agent -> Controller: accepted / rejected
    SessionStart = 5,
    ScreenFrame = 6,
    MouseEvent = 7,
    KeyboardEvent = 8,
    Ping = 9,
    Pong = 10,
    SessionEnd = 11,
    Error = 12
}
