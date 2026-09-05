namespace EventPlatform.Messaging;

/// <summary>
/// Writes integration events to the transactional outbox, stamped with the envelope that says
/// where they came from. They are published after the caller's unit of work commits
/// (SaveChanges), so there is no dual-write.
/// </summary>
/// <remarks>
/// The envelope is read from the ambient <see cref="ICorrelationContext"/>, so a handler publishing
/// an event does nothing to opt in — which is the point. An opt-in correlation id is one that gets
/// forgotten exactly on the path nobody thought about, and a chain with a hole in it is not a chain.
/// </remarks>
/// <param name="dbContext">The owning service's outbox-carrying context.</param>
/// <param name="correlation">The chain of work the current scope belongs to.</param>
internal sealed class OutboxEventPublisher(IOutboxDbContext dbContext, ICorrelationContext correlation)
    : IEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Enqueue(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var type = integrationEvent.GetType();
        var payload = JsonSerializer.Serialize(integrationEvent, type, SerializerOptions);

        // causationId is the message that caused this scope, not the one being written. An event
        // published while handling another names that other as its cause; one published from a
        // person's request names nothing, because the person is the cause and has no message id.
        //
        // Said here rather than against the argument itself: StyleCop wants a blank line above a
        // comment (SA1515) and forbids one between arguments (SA1115), so a comment cannot sit
        // inside an argument list at all.
        var message = new OutboxMessage(
            Guid.CreateVersion7(),
            topic: type.Name,
            type: type.FullName ?? type.Name,
            payload: payload,
            occurredAt: integrationEvent.OccurredAt,
            correlationId: correlation.CorrelationId,
            causationId: correlation.CausationId,
            eventVersion: VersionOf(type));

        dbContext.OutboxMessages.Add(message);
    }

    private static int VersionOf(Type eventType) =>
        eventType.GetCustomAttribute<EventVersionAttribute>()?.Version ?? EventVersionAttribute.Default;
}
