namespace Catalog.Application.Features.PublishEvent;

/// <summary>
/// Handles <see cref="PublishEventCommand"/> by transitioning a draft event to published.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <remarks>
/// TODO (#6): once the transactional outbox is in place, emit an <c>EventPublished</c>
/// integration event here so Inventory can generate seat inventory and Search can index it.
/// </remarks>
internal sealed class PublishEventHandler(IEventRepository repository)
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
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
