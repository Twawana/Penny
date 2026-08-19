using Penny.Protocol;
using Penny.Protocol.Messages;
using Penny.Security;

namespace Penny.Network;

/// <summary>
/// Drives the Agent side of the handshake for one freshly-connected transport:
///
///   Controller --Hello-->        Agent
///   Controller --AuthRequest-->  Agent   (DeviceId + PIN)
///   Agent      --AuthResponse--> Controller (PIN structurally ok? not yet approved)
///   Agent      (invokes RequestUserApprovalAsync — shows Accept/Reject in the Agent UI)
///   Agent      --ConnectionDecision--> Controller
///   [if approved]
///   Agent      --SessionStart--> Controller  (fresh SessionId + SessionToken)
///
/// PIN validation, rate limiting, and the human-in-the-loop approval prompt are
/// all mandatory and in that order — a correct PIN alone never starts a session.
/// </summary>
public sealed class AgentSessionAuthenticator
{
    private readonly PinGenerator _pinGenerator;
    private readonly ConnectionAttemptLimiter _limiter;

    public AgentSessionAuthenticator(PinGenerator pinGenerator, ConnectionAttemptLimiter limiter)
    {
        _pinGenerator = pinGenerator;
        _limiter = limiter;
    }

    /// <summary>The PIN currently displayed on the Agent UI. Replaced by RotatePin.</summary>
    public SessionPin CurrentPin { get; private set; } = default!;

    /// <summary>Must be called once at Agent startup (and after every session ends) to issue a fresh PIN.</summary>
    public SessionPin RotatePin() => CurrentPin = _pinGenerator.GenerateNew();

    /// <summary>
    /// Supplied by Penny.Agent's UI layer: shown the Controller's display name and
    /// asked to Accept/Reject. Must not auto-approve — this is the human authorization
    /// gate described in the security architecture.
    /// </summary>
    public required Func<string /*controllerDisplayName*/, CancellationToken, Task<bool>> RequestUserApprovalAsync { get; init; }

    public async Task<AgentHandshakeResult> RunAsync(
        IPennyTransport transport,
        string localDeviceId,
        CancellationToken ct)
    {
        // 1. Hello
        var (kind, payload) = await transport.ReceiveFrameAsync(ct).ConfigureAwait(false);
        var helloEnvelope = ExpectEnvelope(kind, payload, MessageType.Hello);
        var hello = ProtocolSerializer.DeserializePayload<HelloPayload>(helloEnvelope.PayloadJson);
        if (hello.ClientKind != "Controller")
        {
            return AgentHandshakeResult.Failed("UnexpectedClientKind");
        }

        // 2. AuthRequest
        (kind, payload) = await transport.ReceiveFrameAsync(ct).ConfigureAwait(false);
        var authEnvelope = ExpectEnvelope(kind, payload, MessageType.AuthRequest);
        var authRequest = ProtocolSerializer.DeserializePayload<AuthRequestPayload>(authEnvelope.PayloadJson);

        if (!_limiter.IsAllowed(transport.RemoteEndpoint))
        {
            await SendAuthResponseAsync(transport, authEnvelope.CorrelationId, false, "RateLimited", ct).ConfigureAwait(false);
            return AgentHandshakeResult.Failed("RateLimited");
        }

        bool deviceIdMatches = string.Equals(authRequest.TargetDeviceId, localDeviceId, StringComparison.Ordinal);
        bool pinMatches = deviceIdMatches && CurrentPin is not null && !CurrentPin.IsExpired() && CurrentPin.Matches(authRequest.Pin);

        if (!pinMatches)
        {
            _limiter.RecordFailure(transport.RemoteEndpoint);
            string reason = !deviceIdMatches ? "UnknownDevice" : (CurrentPin?.IsExpired() ?? true) ? "PinExpired" : "InvalidPin";
            await SendAuthResponseAsync(transport, authEnvelope.CorrelationId, false, reason, ct).ConfigureAwait(false);
            return AgentHandshakeResult.Failed(reason);
        }

        _limiter.Reset(transport.RemoteEndpoint);
        await SendAuthResponseAsync(transport, authEnvelope.CorrelationId, true, null, ct).ConfigureAwait(false);

        // 3. Human approval — mandatory, cannot be skipped by protocol data alone.
        bool approved = await RequestUserApprovalAsync(authRequest.ControllerDisplayName, ct).ConfigureAwait(false);

        var decisionEnvelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.ConnectionDecision,
            Sequence = 0,
            TimestampUtc = DateTimeOffset.UtcNow,
            CorrelationId = authEnvelope.CorrelationId,
            PayloadJson = ProtocolSerializer.SerializePayload(new ConnectionDecisionPayload
            {
                Approved = approved,
                Reason = approved ? null : "RejectedByUser"
            })
        };
        await transport.SendEnvelopeAsync(decisionEnvelope, ct).ConfigureAwait(false);

        if (!approved)
        {
            return AgentHandshakeResult.Failed("RejectedByUser");
        }

        // PIN is single-use: rotate immediately so it cannot be replayed for a second session.
        RotatePin();

        var sessionId = Guid.NewGuid();
        var sessionToken = SessionTokenGenerator.Generate();
        return AgentHandshakeResult.Success(sessionId, sessionToken, authRequest.ControllerDisplayName);
    }

    private static async Task SendAuthResponseAsync(
        IPennyTransport transport, Guid? correlationId, bool accepted, string? reason, CancellationToken ct)
    {
        var envelope = new ProtocolEnvelope
        {
            Version = ProtocolSerializer.CurrentVersion,
            Type = MessageType.AuthResponse,
            Sequence = 0,
            TimestampUtc = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            PayloadJson = ProtocolSerializer.SerializePayload(new AuthResponsePayload
            {
                PinAccepted = accepted,
                FailureReason = reason
            })
        };
        await transport.SendEnvelopeAsync(envelope, ct).ConfigureAwait(false);
    }

    private static ProtocolEnvelope ExpectEnvelope(byte kind, byte[] payload, MessageType expected)
    {
        if (kind != FrameCodec.KindEnvelope)
        {
            throw new ProtocolException($"Expected a control envelope but received binary frame kind 0x{kind:X2}.");
        }
        var json = System.Text.Encoding.UTF8.GetString(payload);
        var envelope = ProtocolSerializer.DeserializeEnvelope(json);
        if (envelope.Type != expected)
        {
            throw new ProtocolException($"Expected {expected} but received {envelope.Type}.");
        }
        return envelope;
    }
}

public sealed record AgentHandshakeResult
{
    public required bool IsApproved { get; init; }
    public Guid? SessionId { get; init; }
    public string? SessionToken { get; init; }
    public string? ControllerDisplayName { get; init; }
    public string? FailureReason { get; init; }

    public static AgentHandshakeResult Success(Guid sessionId, string sessionToken, string controllerDisplayName) => new()
    {
        IsApproved = true,
        SessionId = sessionId,
        SessionToken = sessionToken,
        ControllerDisplayName = controllerDisplayName
    };

    public static AgentHandshakeResult Failed(string reason) => new()
    {
        IsApproved = false,
        FailureReason = reason
    };
}
