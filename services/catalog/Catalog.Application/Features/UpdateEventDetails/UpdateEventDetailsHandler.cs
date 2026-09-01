namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>
/// Handles <see cref="UpdateEventDetailsCommand"/> by setting a draft event's dates, venue and
/// pricing rules and enqueuing an <see cref="EventUpdated"/> integration event in the same unit of
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

        if (request.BookingEndsAt is not null && request.BookingEndsAt > request.StartsAt)
        {
            return UpdateEventDetailsOutcome.BookingCutoffAfterStart;
        }

        if (@event.EventGroupId is { } eventGroupId)
        {
            var group = await eventGroupRepository.GetByIdAsync(eventGroupId, cancellationToken);
            if (group is not null)
            {
                // Checked against the *incoming* dates, not the stored ones: the start time is now
                // editable here, so validating the persisted value would let an edit walk a leg
                // straight out of its tour's advertised range.
                if ((group.StartsAt is not null && request.StartsAt < group.StartsAt)
                    || (group.EndsAt is not null && request.EndsAt > group.EndsAt))
                {
                    return UpdateEventDetailsOutcome.OutsideEventGroupRange;
                }

                var siblingLegs = await repository.ListLegsForEventGroupAsync(eventGroupId, cancellationToken);
                var overlaps = siblingLegs.Any(leg =>
                    leg.Id != @event.Id && leg.StartsAt < request.EndsAt && request.StartsAt < leg.EndsAt);
                if (overlaps)
                {
                    return UpdateEventDetailsOutcome.OverlapsExistingLeg;
                }
            }
        }

        @event.UpdateSchedule(
            request.StartsAt,
            request.EndsAt,
            request.DoorsOpenAt,
            request.OnSaleAt,
            request.BookingEndsAt,
            new EventLocation(
                request.LocationName,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.Region,
                request.PostalCode,
                request.Country,
                request.Latitude,
                request.Longitude),
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.TaxRatePercent,
            request.TaxLabel,
            request.BookingFeePerTicketMinor,
            request.TimeZoneId);

        events.Enqueue(new EventUpdated(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id));

        await repository.SaveChangesAsync(cancellationToken);
        return UpdateEventDetailsOutcome.Updated;
    }
}
