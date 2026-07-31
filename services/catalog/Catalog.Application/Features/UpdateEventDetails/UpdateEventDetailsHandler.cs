namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>
/// Handles <see cref="UpdateEventDetailsCommand"/> by setting a draft event's descriptive
/// details and enqueuing an <see cref="EventUpdated"/> integration event in the same unit of
/// work — reusing <c>PublishEvent</c>'s existing outbox pattern.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class UpdateEventDetailsHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<UpdateEventDetailsCommand, UpdateEventDetailsOutcome>
{
    /// <inheritdoc />
    public async Task<UpdateEventDetailsOutcome> Handle(UpdateEventDetailsCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return UpdateEventDetailsOutcome.NotFound;
        }

        if (@event.Status != EventStatus.Draft)
        {
            return UpdateEventDetailsOutcome.NotDraft;
        }

        @event.UpdateDetails(
            request.Description,
            request.Category,
            request.EndsAt,
            request.DoorsOpenAt,
            request.OnSaleAt,
            request.OffSaleAt,
            request.AgeRestriction,
            request.BannerImageUrl,
            request.VideoUrl);

        events.Enqueue(new EventUpdated(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id));

        await repository.SaveChangesAsync(cancellationToken);
        return UpdateEventDetailsOutcome.Updated;
    }
}
