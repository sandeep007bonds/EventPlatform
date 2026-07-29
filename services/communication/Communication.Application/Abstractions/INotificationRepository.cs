namespace Communication.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the delivery log and the inbound-event dedup ledger. Implemented in
/// the Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface INotificationRepository
{
    /// <summary>Registers a delivery-log row to be persisted.</summary>
    /// <param name="entry">The delivery-log entry to add.</param>
    void AddDeliveryLog(DeliveryLogEntry entry);

    /// <summary>Returns whether an integration event has already been processed (redelivery dedupe).</summary>
    /// <param name="eventId">The integration event's id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the event was already processed.</returns>
    Task<bool> HasProcessedEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Records an integration event as processed.</summary>
    /// <param name="eventId">The integration event's id.</param>
    /// <param name="eventType">The event's type name, for diagnostics.</param>
    void RecordProcessedEvent(Guid eventId, string eventType);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
