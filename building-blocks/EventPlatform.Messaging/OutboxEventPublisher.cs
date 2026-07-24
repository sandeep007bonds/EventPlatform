namespace EventPlatform.Messaging;

/// <summary>
/// Writes integration events to the transactional outbox. They are published after the
/// caller's unit of work commits (SaveChanges), so there is no dual-write.
/// </summary>
/// <param name="dbContext">The outbox-owning DbContext.</param>
internal sealed class OutboxEventPublisher(IOutboxDbContext dbContext) : IEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Enqueue(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var type = integrationEvent.GetType();
        var payload = JsonSerializer.Serialize(integrationEvent, type, SerializerOptions);

        var message = new OutboxMessage(
            Guid.CreateVersion7(),
            topic: type.Name,
            type: type.FullName ?? type.Name,
            payload: payload,
            occurredAt: integrationEvent.OccurredAt);

        dbContext.OutboxMessages.Add(message);
    }
}
