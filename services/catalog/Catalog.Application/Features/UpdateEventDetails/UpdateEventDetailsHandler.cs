namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>
/// Handles <see cref="UpdateEventDetailsCommand"/> by setting a draft event's descriptive
/// details and enqueuing an <see cref="EventUpdated"/> integration event in the same unit of
/// work — reusing <c>PublishEvent</c>'s existing outbox pattern.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="eventGroupRepository">The event-group repository, for tour-range validation.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class UpdateEventDetailsHandler(
    IEventRepository repository,
    IEventGroupRepository eventGroupRepository,
    IEventPublisher events)
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

        if (request.BookingEndsAt is not null && request.BookingEndsAt > @event.StartsAt)
        {
            return UpdateEventDetailsOutcome.BookingCutoffAfterStart;
        }

        if (@event.EventGroupId is { } eventGroupId)
        {
            var group = await eventGroupRepository.GetByIdAsync(eventGroupId, cancellationToken);
            if (group is not null)
            {
                if ((group.StartsAt is not null && @event.StartsAt < group.StartsAt)
                    || (group.EndsAt is not null && request.EndsAt > group.EndsAt))
                {
                    return UpdateEventDetailsOutcome.OutsideEventGroupRange;
                }

                var siblingLegs = await repository.ListLegsForEventGroupAsync(eventGroupId, cancellationToken);
                var overlaps = siblingLegs.Any(leg =>
                    leg.Id != @event.Id && leg.StartsAt < request.EndsAt && @event.StartsAt < leg.EndsAt);
                if (overlaps)
                {
                    return UpdateEventDetailsOutcome.OverlapsExistingLeg;
                }
            }
        }

        @event.UpdateDetails(
            request.Description,
            request.Category,
            request.EndsAt,
            request.DoorsOpenAt,
            request.OnSaleAt,
            request.BookingEndsAt,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.AgeRestriction,
            request.BannerImageUrl,
            request.VideoUrl,
            request.ContactPhone,
            request.ContactMobile,
            request.ContactEmail,
            request.WebsiteUrl,
            request.SocialLinks.Select(link => (link.Platform, link.Url)));

        events.Enqueue(new EventUpdated(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id));

        await repository.SaveChangesAsync(cancellationToken);
        return UpdateEventDetailsOutcome.Updated;
    }
}
