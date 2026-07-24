namespace EventPlatform.Contracts;

/// <summary>
/// Enqueues an integration event for publication via the transactional outbox.
/// The event is published after the caller's unit of work commits.
/// </summary>
public interface IEventPublisher
{
    /// <summary>Adds an integration event to the outbox.</summary>
    /// <param name="integrationEvent">The event to enqueue.</param>
    void Enqueue(IntegrationEvent integrationEvent);
}
