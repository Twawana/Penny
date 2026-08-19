using Penny.Protocol;
using Penny.Protocol.Messages;
using Xunit;

namespace Penny.Protocol.Tests;

public class ProtocolSerializerTests
{
    [Fact]
    public void EnvelopeRoundTrips_ThroughJson()
    {
        var payload = new MouseEventPayload
        {
            Kind = MouseEventKind.Move,
            NormalizedX = 0.42,
            NormalizedY = 0.73
        };

        var envelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.MouseEvent,
            Sequence = 7,
            TimestampUtc = DateTimeOffset.UtcNow,
            PayloadJson = ProtocolSerializer.SerializePayload(payload)
        };

        var json = ProtocolSerializer.SerializeEnvelope(envelope);
        var roundTripped = ProtocolSerializer.DeserializeEnvelope(json);

        Assert.Equal(envelope.Type, roundTripped.Type);
        Assert.Equal(envelope.Sequence, roundTripped.Sequence);

        var roundTrippedPayload = ProtocolSerializer.DeserializePayload<MouseEventPayload>(roundTripped.PayloadJson);
        Assert.Equal(payload.NormalizedX, roundTrippedPayload.NormalizedX);
        Assert.Equal(payload.Kind, roundTrippedPayload.Kind);
    }

    [Fact]
    public void DeserializePayload_ThrowsOnOversizedInput()
    {
        var huge = new string('a', ProtocolSerializer.MaxPayloadJsonBytes + 10);
        Assert.Throws<ProtocolException>(() => ProtocolSerializer.DeserializePayload<ErrorPayload>(huge));
    }
}

public class FrameCodecTests
{
    [Fact]
    public async Task WriteAndReadEnvelope_RoundTrips()
    {
        using var stream = new MemoryStream();

        var envelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.Ping,
            Sequence = 1,
            TimestampUtc = DateTimeOffset.UtcNow,
            PayloadJson = ProtocolSerializer.SerializePayload(new PingPayload { SentAtUtc = DateTimeOffset.UtcNow })
        };

        await FrameCodec.WriteEnvelopeAsync(stream, envelope, CancellationToken.None);
        stream.Position = 0;

        var (kind, payload) = await FrameCodec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal(FrameCodec.KindEnvelope, kind);
        var decoded = ProtocolSerializer.DeserializeEnvelope(System.Text.Encoding.UTF8.GetString(payload));
        Assert.Equal(MessageType.Ping, decoded.Type);
    }

    [Fact]
    public async Task WriteAndReadBinaryFrame_RoundTrips()
    {
        using var stream = new MemoryStream();
        byte[] fakeJpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5 };

        await FrameCodec.WriteBinaryFrameAsync(stream, fakeJpegBytes, CancellationToken.None);
        stream.Position = 0;

        var (kind, payload) = await FrameCodec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal(FrameCodec.KindBinaryFrame, kind);
        Assert.Equal(fakeJpegBytes, payload);
    }

    [Fact]
    public async Task MultipleFrames_ReadBackInOrder()
    {
        using var stream = new MemoryStream();

        var envelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.SessionEnd,
            Sequence = 2,
            TimestampUtc = DateTimeOffset.UtcNow,
            PayloadJson = ProtocolSerializer.SerializePayload(new SessionEndPayload { Reason = "UserDisconnected" })
        };
        byte[] binaryData = { 9, 9, 9 };

        await FrameCodec.WriteEnvelopeAsync(stream, envelope, CancellationToken.None);
        await FrameCodec.WriteBinaryFrameAsync(stream, binaryData, CancellationToken.None);
        stream.Position = 0;

        var first = await FrameCodec.ReadFrameAsync(stream, CancellationToken.None);
        var second = await FrameCodec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal(FrameCodec.KindEnvelope, first.Kind);
        Assert.Equal(FrameCodec.KindBinaryFrame, second.Kind);
        Assert.Equal(binaryData, second.Payload);
    }

    [Fact]
    public async Task ReadFrameAsync_ThrowsOnTruncatedStream()
    {
        using var stream = new MemoryStream(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x10 }); // says 16 bytes follow, but none do
        await Assert.ThrowsAsync<ProtocolException>(() => FrameCodec.ReadFrameAsync(stream, CancellationToken.None));
    }
}
