namespace Communication.Infrastructure;

/// <summary>EF Core implementation of <see cref="INotificationRepository"/>.</summary>
/// <param name="dbContext">The Communication database context.</param>
internal sealed class NotificationRepository(CommunicationDbContext dbContext) : INotificationRepository
{
    /// <inheritdoc />
    public void AddDeliveryLog(DeliveryLogEntry entry) => dbContext.DeliveryLog.Add(entry);

    /// <inheritdoc />
    public Task<bool> HasProcessedEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.ProcessedNotificationEvents.AnyAsync(e => e.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public void RecordProcessedEvent(Guid eventId, string eventType) =>
        dbContext.ProcessedNotificationEvents.Add(ProcessedNotificationEvent.Record(eventId, eventType));

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
