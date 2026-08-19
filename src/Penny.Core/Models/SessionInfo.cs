namespace Penny.Core.Models;

/// <summary>
/// Describes an active or pending remote-control session between a specific
/// Controller and a specific Agent. Holds no secrets — the session token itself
/// lives only in Penny.Security and in memory on each endpoint.
/// </summary>
public sealed record SessionInfo
{
    /// <summary>Server-issued session identifier (GUID), unique per connection attempt.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>DeviceId of the Agent being controlled.</summary>
    public required string AgentDeviceId { get; init; }

    /// <summary>Display name of the connecting Controller, shown in the Agent's approval prompt.</summary>
    public required string ControllerDisplayName { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public ConnectionState State { get; init; } = ConnectionState.Idle;

    /// <summary>Wall-clock duration of the active session, for UI display (e.g. "00:14:32").</summary>
    public TimeSpan Elapsed(DateTimeOffset nowUtc) => nowUtc - CreatedAtUtc;
}
