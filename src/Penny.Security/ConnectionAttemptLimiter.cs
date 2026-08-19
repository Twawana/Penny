using System.Collections.Concurrent;

namespace Penny.Security;

/// <summary>
/// Simple sliding-window rate limiter for inbound auth attempts on the Agent.
/// Prevents PIN brute-forcing: after <see cref="MaxAttempts"/> failed attempts
/// within <see cref="Window"/>, the source is locked out until the window rolls
/// over. Keyed by remote endpoint (IP) — the Agent's TCP listener supplies this.
/// </summary>
public sealed class ConnectionAttemptLimiter
{
    private readonly ConcurrentDictionary<string, Attempts> _byKey = new();

    public int MaxAttempts { get; init; } = 5;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(2);

    private sealed class Attempts
    {
        public int Count;
        public DateTimeOffset WindowStartUtc;
    }

    /// <summary>Returns true if the key is currently allowed to attempt authentication.</summary>
    public bool IsAllowed(string key)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = _byKey.GetOrAdd(key, _ => new Attempts { Count = 0, WindowStartUtc = now });
        lock (entry)
        {
            if (now - entry.WindowStartUtc > Window)
            {
                entry.Count = 0;
                entry.WindowStartUtc = now;
            }
            return entry.Count < MaxAttempts;
        }
    }

    /// <summary>Records a failed attempt for the key. Call only on authentication failure.</summary>
    public void RecordFailure(string key)
    {
        var now = DateTimeOffset.UtcNow;
        var entry = _byKey.GetOrAdd(key, _ => new Attempts { Count = 0, WindowStartUtc = now });
        lock (entry)
        {
            if (now - entry.WindowStartUtc > Window)
            {
                entry.Count = 0;
                entry.WindowStartUtc = now;
            }
            entry.Count++;
        }
    }

    /// <summary>Clears attempt history for the key, e.g. after a successful auth.</summary>
    public void Reset(string key) => _byKey.TryRemove(key, out _);
}
