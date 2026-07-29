namespace Communication.Infrastructure;

/// <summary>
/// An integration event that has been processed, forming the idempotency ledger that turns Dapr
/// pub/sub's at-least-once delivery into exactly-once handling. Keyed by the contract-level
/// <c>IntegrationEvent.EventId</c> (a <see cref="Guid"/>) — unlike Payments' <c>ProcessedWebhookEvent</c>,
/// which dedupes on a vendor-supplied <see cref="string"/> id.
/// </summary>
internal sealed class ProcessedNotificationEvent
{
    private ProcessedNotificationEvent()
    {
    }

    private ProcessedNotificationEvent(Guid eventId, string eventType, DateTimeOffset processedAt)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }

    /// <summary>The integration event's unique id (primary key).</summary>
    public Guid EventId { get; private set; }

    /// <summary>The event's type name, kept for diagnostics.</summary>
    public string EventType { get; private set; } = default!;

    /// <summary>When the event was processed (UTC).</summary>
    public DateTimeOffset ProcessedAt { get; private set; }

    /// <summary>Creates a processed-event record stamped with the current time.</summary>
    /// <param name="eventId">The integration event's unique id.</param>
    /// <param name="eventType">The event's type name.</param>
    /// <returns>A new <see cref="ProcessedNotificationEvent"/>.</returns>
    public static ProcessedNotificationEvent Record(Guid eventId, string eventType) =>
        new(eventId, eventType, DateTimeOffset.UtcNow);
}
