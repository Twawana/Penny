using System.Security.Cryptography;

namespace Penny.Security;

/// <summary>
/// Generates and validates the short-lived numeric PIN the Agent displays and
/// the Controller must supply to request a connection.
///
/// The PIN is:
///  - 6 digits, generated with a CSPRNG (not Random/Guid).
///  - Time-limited (default 5 minutes) and single-use per connection attempt.
///  - Rotated automatically on expiry, on session end, and on demand.
///  - Rate-limited by the caller (see SessionAuthenticator) to prevent brute force.
///
/// The PIN is a second factor alongside explicit Accept/Reject on the Agent —
/// knowing a correct PIN triggers an approval *prompt*, it never grants access
/// by itself.
/// </summary>
public sealed class PinGenerator
{
    private readonly TimeSpan _validity;

    public PinGenerator(TimeSpan? validity = null)
    {
        _validity = validity ?? TimeSpan.FromMinutes(5);
    }

    public SessionPin GenerateNew()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);
        uint value = BitConverter.ToUInt32(buffer) % 1_000_000u;
        string pin = value.ToString("D6");
        var now = DateTimeOffset.UtcNow;
        return new SessionPin(pin, now, now.Add(_validity));
    }
}

/// <summary>An issued PIN plus its validity window. Never serialize this to disk or logs.</summary>
public sealed record SessionPin(string Value, DateTimeOffset IssuedAtUtc, DateTimeOffset ExpiresAtUtc)
{
    public bool IsExpired(DateTimeOffset? nowUtc = null) => (nowUtc ?? DateTimeOffset.UtcNow) >= ExpiresAtUtc;

    /// <summary>Constant-time comparison to avoid timing side-channels on PIN checks.</summary>
    public bool Matches(string candidate)
    {
        if (candidate is null) return false;
        var expected = System.Text.Encoding.ASCII.GetBytes(Value);
        var actual = System.Text.Encoding.ASCII.GetBytes(candidate.Trim());
        if (expected.Length != actual.Length) return false;
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
