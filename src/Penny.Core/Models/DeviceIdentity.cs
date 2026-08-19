namespace Penny.Core.Models;

/// <summary>
/// Represents the public, non-secret identity of a Penny Agent instance.
/// The Id is a human-friendly identifier (e.g. "583-921-447") used to locate
/// a device on the network or via a future relay/signaling service.
///
/// IMPORTANT: DeviceId is NOT a credential. It is analogous to a phone number
/// or username — assume it can be observed, guessed, or shared. All access
/// control decisions must be made using the short-lived Session PIN and the
/// explicit user-approval step on the Agent, never on DeviceId alone.
/// </summary>
public sealed record DeviceIdentity
{
    /// <summary>Human-friendly device identifier, format "###-###-###".</summary>
    public required string DeviceId { get; init; }

    /// <summary>Free-text machine name shown to the connecting Controller (e.g. "T's Desktop").</summary>
    public required string MachineName { get; init; }

    /// <summary>Penny protocol version implemented by this Agent.</summary>
    public required string ProtocolVersion { get; init; }

    /// <summary>UTC timestamp when this identity/session was generated.</summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }
}
