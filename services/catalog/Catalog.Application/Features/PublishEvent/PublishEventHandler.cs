namespace Catalog.Application.Features.PublishEvent;

/// <summary>
/// Handles <see cref="PublishEventCommand"/> by transitioning a draft event to published and
/// enqueuing an <see cref="EventPublished"/> integration event (carrying the seat count) in the
/// same unit of work. A seated event must have a seat map before it can be published.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="seatMaps">The seat-map repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class PublishEventHandler(
    IEventRepository repository,
    ISeatMapRepository seatMaps,
    IEventPublisher events)
    : IRequestHandler<PublishEventCommand, PublishEventOutcome>
{
    /// <inheritdoc />
    public async Task<PublishEventOutcome> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return PublishEventOutcome.NotFound;
        }

        if (@event.Status != EventStatus.Draft)
        {
            return PublishEventOutcome.NotDraft;
        }

        var seatMap = await seatMaps.GetByEventIdAsync(request.Id, cancellationToken);
        if (seatMap is null)
        {
            return PublishEventOutcome.NoSeatMap;
        }

        @event.Publish();

        events.Enqueue(new EventPublished(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id,
            @event.Title,
            seatMap.Capacity,
            @event.BookingEndsAt,
            @event.StartsAt,
            @event.EndsAt,
            @event.MaxTicketsPerBuyer,
            @event.OnSaleAt,
            @event.DoorsOpenAt));

        await repository.SaveChangesAsync(cancellationToken);
        return PublishEventOutcome.Published;
    }
}
