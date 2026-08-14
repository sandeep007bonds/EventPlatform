namespace Queue.Application.Queueing;

/// <summary>How many new waiting-room sessions one client may create per event, per window.</summary>
public sealed class QueueRateLimitOptions
{
    /// <summary>
    /// New sessions allowed per <see cref="Window"/>. Deliberately generous: shared addresses are
    /// ordinary — a carrier NAT, an office, a stadium's own wi-fi — and a real buyer needs one
    /// session, so the cost of setting this too low (turning away genuine customers at the moment
    /// they are trying to buy) is far worse than letting a script have a handful of places in line.
    /// </summary>
    public int MaxNewSessionsPerWindow { get; set; } = 10;

    /// <summary>The fixed window over which <see cref="MaxNewSessionsPerWindow"/> is counted.</summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
