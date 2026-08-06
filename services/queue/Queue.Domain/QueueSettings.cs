namespace Queue.Domain;

/// <summary>
/// Per-event waiting-room configuration. Provisioned once, idempotently, from Catalog's
/// <c>EventPublished</c> (<see cref="Enabled"/> is set from <c>EventPublished.RequiresQueue</c> at
/// that moment and never re-toggled here — see <see cref="UpdateTuning"/>). An organizer may still
/// tune the pacing knobs afterward, since pacing only matters once an event is actually live.
/// </summary>
public sealed class QueueSettings
{
    private QueueSettings()
    {
    }

    private QueueSettings(
        Guid eventId,
        Guid tenantId,
        bool enabled,
        int admissionRatePerInterval,
        int intervalSeconds,
        int sessionTtlSeconds)
    {
        EventId = eventId;
        TenantId = tenantId;
        Enabled = enabled;
        AdmissionRatePerInterval = admissionRatePerInterval;
        IntervalSeconds = intervalSeconds;
        SessionTtlSeconds = sessionTtlSeconds;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>The Catalog event this configuration belongs to (primary key).</summary>
    public Guid EventId { get; private set; }

    /// <summary>The owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Whether a buyer must pass through the waiting room before placing a hold for this event.
    /// Set once at provisioning time from <c>EventPublished.RequiresQueue</c> — Catalog's
    /// <c>Event.RequiresQueue</c> is the single on/off source of truth; this service never exposes
    /// its own independent toggle, to avoid two places disagreeing about whether queueing is on.
    /// </summary>
    public bool Enabled { get; private set; }

    /// <summary>How many waiting sessions are admitted every <see cref="IntervalSeconds"/>.</summary>
    public int AdmissionRatePerInterval { get; private set; }

    /// <summary>How often the admission controller promotes waiting sessions for this event.</summary>
    public int IntervalSeconds { get; private set; }

    /// <summary>
    /// How long an admission token stays valid once minted, in seconds — the window a buyer has to
    /// complete a hold before losing their admitted spot.
    /// </summary>
    public int SessionTtlSeconds { get; private set; }

    /// <summary>When this configuration was first provisioned.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the pacing knobs were last tuned.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Provisions a new event's queue settings, straight from <c>EventPublished</c>.</summary>
    /// <param name="eventId">The Catalog event id.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="enabled">Whether the event requires queueing — <c>EventPublished.RequiresQueue</c>.</param>
    /// <returns>A new <see cref="QueueSettings"/> with sensible pacing defaults.</returns>
    public static QueueSettings Create(Guid eventId, Guid tenantId, bool enabled) =>
        new(eventId, tenantId, enabled, admissionRatePerInterval: 50, intervalSeconds: 10, sessionTtlSeconds: 600);

    /// <summary>
    /// Adjusts the pacing knobs — deliberately does not accept an <see cref="Enabled"/> value; that
    /// stays fixed at whatever <c>EventPublished.RequiresQueue</c> said at provisioning time.
    /// </summary>
    /// <param name="admissionRatePerInterval">Sessions admitted per interval. Must be positive.</param>
    /// <param name="intervalSeconds">Seconds between admission passes. Must be positive.</param>
    /// <param name="sessionTtlSeconds">Admission-token lifetime, in seconds. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Any argument is not positive.</exception>
    public void UpdateTuning(int admissionRatePerInterval, int intervalSeconds, int sessionTtlSeconds)
    {
        if (admissionRatePerInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(admissionRatePerInterval), "Must be positive.");
        }

        if (intervalSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Must be positive.");
        }

        if (sessionTtlSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionTtlSeconds), "Must be positive.");
        }

        AdmissionRatePerInterval = admissionRatePerInterval;
        IntervalSeconds = intervalSeconds;
        SessionTtlSeconds = sessionTtlSeconds;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
