using System.Buffers.Binary;
using System.Text;

namespace Penny.Protocol;

/// <summary>
/// Encodes/decodes both kinds of data Penny sends over its single TCP+TLS
/// stream per connection, distinguished by a 1-byte frame kind:
///
///   [1 byte kind][4 bytes big-endian length][payload bytes]
///
///   kind = 0x01  -> payload is UTF-8 JSON for a ProtocolEnvelope (control message)
///   kind = 0x02  -> payload is raw encoded screen-frame bytes (JPEG/H.264), and the
///                    envelope describing it (ScreenFrameMetaPayload) is always the
///                    *immediately preceding* kind=0x01 frame on the stream.
///
/// A single length-prefixed framing scheme (rather than newline-delimited JSON)
/// means control messages and binary frame bytes can share one stream safely —
/// no delimiter collision is possible, and readers never need to buffer-scan.
/// </summary>
public static class FrameCodec
{
    public const byte KindEnvelope = 0x01;
    public const byte KindBinaryFrame = 0x02;

    /// <summary>Hard cap on any single frame (covers worst-case 4K screen frame at MVP JPEG quality with margin).</summary>
    public const int MaxFrameBytes = 8 * 1024 * 1024; // 8 MB

    public static async Task WriteEnvelopeAsync(Stream stream, ProtocolEnvelope envelope, CancellationToken ct)
    {
        var json = ProtocolSerializer.SerializeEnvelope(envelope);
        var bytes = Encoding.UTF8.GetBytes(json);
        await WriteFrameAsync(stream, KindEnvelope, bytes, ct).ConfigureAwait(false);
    }

    public static async Task WriteBinaryFrameAsync(Stream stream, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        await WriteFrameAsync(stream, KindBinaryFrame, data, ct).ConfigureAwait(false);
    }

    private static async Task WriteFrameAsync(Stream stream, byte kind, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        if (payload.Length > MaxFrameBytes)
        {
            throw new ProtocolException($"Outgoing frame of {payload.Length} bytes exceeds MaxFrameBytes ({MaxFrameBytes}).");
        }

        var header = new byte[5];
        header[0] = kind;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(1, 4), payload.Length);

        await stream.WriteAsync(header, ct).ConfigureAwait(false);
        if (payload.Length > 0)
        {
            await stream.WriteAsync(payload, ct).ConfigureAwait(false);
        }
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads exactly one frame. Returns (kind, payload). Throws ProtocolException on malformed/oversized input.</summary>
    public static async Task<(byte Kind, byte[] Payload)> ReadFrameAsync(Stream stream, CancellationToken ct)
    {
        var header = await ReadExactAsync(stream, 5, ct).ConfigureAwait(false);
        byte kind = header[0];
        int length = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(1, 4));

        if (length < 0 || length > MaxFrameBytes)
        {
            throw new ProtocolException($"Incoming frame length {length} is invalid or exceeds MaxFrameBytes ({MaxFrameBytes}).");
        }
        if (kind != KindEnvelope && kind != KindBinaryFrame)
        {
            throw new ProtocolException($"Unknown frame kind 0x{kind:X2}.");
        }

        var payload = length == 0 ? Array.Empty<byte>() : await ReadExactAsync(stream, length, ct).ConfigureAwait(false);
        return (kind, payload);
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct).ConfigureAwait(false);
            if (read == 0)
            {
                throw new ProtocolException("Stream closed before expected frame data was fully read.");
            }
            offset += read;
        }
        return buffer;
    }
}
