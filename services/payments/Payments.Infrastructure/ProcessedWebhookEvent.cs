namespace Payments.Infrastructure;

/// <summary>
/// A provider webhook event that has been processed, forming the idempotency ledger that turns
/// at-least-once delivery into exactly-once processing. Keyed by the provider's event id.
/// </summary>
internal sealed class ProcessedWebhookEvent
{
    private ProcessedWebhookEvent()
    {
    }

    private ProcessedWebhookEvent(string eventId, string eventType, DateTimeOffset processedAt)
    {
        EventId = eventId;
        EventType = eventType;
        ProcessedAt = processedAt;
    }

    /// <summary>The provider's unique event id (primary key).</summary>
    public string EventId { get; private set; } = default!;

    /// <summary>The raw provider event type, kept for diagnostics.</summary>
    public string EventType { get; private set; } = default!;

    /// <summary>When the event was processed (UTC).</summary>
    public DateTimeOffset ProcessedAt { get; private set; }

    /// <summary>Creates a processed-webhook record stamped with the current time.</summary>
    /// <param name="eventId">The provider's unique event id.</param>
    /// <param name="eventType">The raw provider event type.</param>
    /// <returns>A new <see cref="ProcessedWebhookEvent"/>.</returns>
    public static ProcessedWebhookEvent Record(string eventId, string eventType) =>
        new(eventId, eventType, DateTimeOffset.UtcNow);
}
