namespace Catalog.Application.Features.PublishEvent;

/// <summary>
/// Handles <see cref="PublishEventCommand"/> by transitioning a draft event to published and
/// enqueuing an <see cref="EventPublished"/> integration event in the same unit of work.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class PublishEventHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<PublishEventCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null)
        {
            return false;
        }

        @event.Publish();

        events.Enqueue(new EventPublished(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id,
            @event.Title));

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
