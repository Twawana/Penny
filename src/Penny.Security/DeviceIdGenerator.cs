using System.Security.Cryptography;

namespace Penny.Security;

/// <summary>
/// Generates human-friendly Device IDs in the form "###-###-###".
///
/// SECURITY NOTE: DeviceId is an *identifier*, not a credential. It is generated
/// with a cryptographically secure RNG so it cannot be trivially enumerated,
/// but it is short enough (9 digits, ~30 bits) that it must never be treated as
/// a secret on its own. Access control always also requires SessionPin
/// (see PinGenerator) plus explicit user approval on the Agent.
/// </summary>
public static class DeviceIdGenerator
{
    /// <summary>Generates a new "###-###-###" device id using a CSPRNG.</summary>
    public static string Generate()
    {
        Span<byte> buffer = stackalloc byte[4];
        RandomNumberGenerator.Fill(buffer);
        // 30-bit space is plenty for a human-typed identifier and keeps the
        // three-group format the UI mock-up expects.
        uint value = BitConverter.ToUInt32(buffer) % 1_000_000_000u;
        string digits = value.ToString("D9");
        return $"{digits[..3]}-{digits[3..6]}-{digits[6..9]}";
    }

    /// <summary>Validates the "###-###-###" format without asserting the id exists.</summary>
    public static bool IsWellFormed(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        var parts = candidate.Split('-');
        if (parts.Length != 3) return false;
        foreach (var part in parts)
        {
            if (part.Length != 3) return false;
            foreach (var c in part)
            {
                if (!char.IsAsciiDigit(c)) return false;
            }
        }
        return true;
    }
}
