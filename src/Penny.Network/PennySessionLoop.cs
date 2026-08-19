using System.Text;
using Penny.Protocol;
using Penny.Protocol.Messages;

namespace Penny.Network;

/// <summary>
/// Minimal control loop on an established connection. Handles Ping/Pong and SessionEnd until disconnect.
/// </summary>
public static class PennySessionLoop
{
    public static async Task RunAgentAsync(IPennyTransport transport, CancellationToken ct)
    {
        ulong sequence = 2;

        while (!ct.IsCancellationRequested)
        {
            var (kind, payload) = await transport.ReceiveFrameAsync(ct).ConfigureAwait(false);
            if (kind != FrameCodec.KindEnvelope)
            {
                continue;
            }

            var envelope = ProtocolSerializer.DeserializeEnvelope(Encoding.UTF8.GetString(payload));
            switch (envelope.Type)
            {
                case MessageType.Ping:
                {
                    var ping = ProtocolSerializer.DeserializePayload<PingPayload>(envelope.PayloadJson);
                    var pongEnvelope = new ProtocolEnvelope
                    {
                        Version = ProtocolSerializer.CurrentVersion,
                        Type = MessageType.Pong,
                        Sequence = sequence++,
                        TimestampUtc = DateTimeOffset.UtcNow,
                        CorrelationId = envelope.CorrelationId,
                        PayloadJson = ProtocolSerializer.SerializePayload(new PongPayload
                        {
                            EchoedSentAtUtc = ping.SentAtUtc,
                            RepliedAtUtc = DateTimeOffset.UtcNow
                        })
                    };
                    await transport.SendEnvelopeAsync(pongEnvelope, ct).ConfigureAwait(false);
                    break;
                }
                case MessageType.SessionEnd:
                    return;
                default:
                    break;
            }
        }
    }

    public static async Task SendPingAsync(IPennyTransport transport, ulong sequence, CancellationToken ct)
    {
        var correlationId = Guid.NewGuid();
        var envelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.Ping,
            Sequence = sequence,
            TimestampUtc = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            PayloadJson = ProtocolSerializer.SerializePayload(new PingPayload
            {
                SentAtUtc = DateTimeOffset.UtcNow
            })
        };

        await transport.SendEnvelopeAsync(envelope, ct).ConfigureAwait(false);

        var (kind, payload) = await transport.ReceiveFrameAsync(ct).ConfigureAwait(false);
        if (kind != FrameCodec.KindEnvelope)
        {
            throw new ProtocolException($"Expected Pong envelope but received kind 0x{kind:X2}.");
        }

        var response = ProtocolSerializer.DeserializeEnvelope(Encoding.UTF8.GetString(payload));
        if (response.Type != MessageType.Pong)
        {
            throw new ProtocolException($"Expected Pong but received {response.Type}.");
        }
    }

    public static async Task SendSessionEndAsync(IPennyTransport transport, ulong sequence, string reason, CancellationToken ct)
    {
        var envelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.SessionEnd,
            Sequence = sequence,
            TimestampUtc = DateTimeOffset.UtcNow,
            PayloadJson = ProtocolSerializer.SerializePayload(new SessionEndPayload
            {
                Reason = reason
            })
        };

        await transport.SendEnvelopeAsync(envelope, ct).ConfigureAwait(false);
    }
}
