using System.Security.Cryptography;

namespace Penny.Security;

/// <summary>
/// Issues a high-entropy session token once the Agent has approved a connection.
/// This token (not the PIN, not the DeviceId) is what authorizes every subsequent
/// message on the connection — it is bound to a single SessionId and is never
/// logged, persisted, or reused across sessions.
/// </summary>
public static class SessionTokenGenerator
{
    /// <summary>256-bit random token, base64url-encoded, suitable for a bearer-style session credential.</summary>
    public static string Generate()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool Matches(string expectedToken, string candidateToken)
    {
        if (expectedToken is null || candidateToken is null) return false;
        var expected = System.Text.Encoding.ASCII.GetBytes(expectedToken);
        var actual = System.Text.Encoding.ASCII.GetBytes(candidateToken);
        if (expected.Length != actual.Length) return false;
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
