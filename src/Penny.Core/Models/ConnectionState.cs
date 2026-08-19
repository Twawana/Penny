namespace Penny.Core.Models;

/// <summary>Lifecycle state of a Controller-to-Agent connection, tracked by both sides.</summary>
public enum ConnectionState
{
    Idle,
    Connecting,
    AwaitingAuthorization,
    Authenticating,
    Active,
    Reconnecting,
    Disconnecting,
    Disconnected,
    Rejected,
    Failed
}

/// <summary>Reason a session ended, used for logging and UI messaging — never contains secrets.</summary>
public enum SessionEndReason
{
    UserDisconnected,
    RejectedByAgent,
    AuthenticationFailed,
    Timeout,
    NetworkError,
    AgentShutdown,
    ProtocolError
}
