using System.Text.Json;
using System.Text.Json.Serialization;

namespace Penny.Protocol;

/// <summary>
/// Centralized System.Text.Json configuration for the whole protocol, plus
/// typed helpers so callers never hand-roll (de)serialization or forget the
/// shared options (which would silently break wire compatibility).
/// </summary>
public static class ProtocolSerializer
{
    public const string CurrentVersion = "1.0";

    /// <summary>Hard cap on a single envelope's JSON payload. Rejects malformed/hostile input early.</summary>
    public const int MaxPayloadJsonBytes = 64 * 1024; // 64 KB — generous for control messages, excludes frame pixel data.

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string SerializePayload<T>(T payload) => JsonSerializer.Serialize(payload, Options);

    public static T DeserializePayload<T>(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxPayloadJsonBytes)
        {
            throw new ProtocolException($"Payload exceeds max size of {MaxPayloadJsonBytes} bytes.");
        }
        return JsonSerializer.Deserialize<T>(json, Options)
               ?? throw new ProtocolException($"Payload deserialized to null for type {typeof(T).Name}.");
    }

    public static string SerializeEnvelope(ProtocolEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, Options);

    public static ProtocolEnvelope DeserializeEnvelope(string json)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaxPayloadJsonBytes)
        {
            throw new ProtocolException($"Envelope exceeds max size of {MaxPayloadJsonBytes} bytes.");
        }
        return JsonSerializer.Deserialize<ProtocolEnvelope>(json, Options)
               ?? throw new ProtocolException("Envelope deserialized to null.");
    }
}

/// <summary>Thrown for any structurally invalid or oversized protocol data. Always treated as fatal for the connection.</summary>
public sealed class ProtocolException : Exception
{
    public ProtocolException(string message) : base(message) { }
    public ProtocolException(string message, Exception inner) : base(message, inner) { }
}
