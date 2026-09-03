namespace Catalog.Application.Features.CreateEvent;

/// <summary>Handles <see cref="CreateEventCommand"/> by creating a draft event with its first performance.</summary>
/// <param name="repository">The event repository.</param>
/// <param name="eventGroupRepository">The event-group repository, for tour-range/ownership validation.</param>
internal sealed class CreateEventHandler(IEventRepository repository, IEventGroupRepository eventGroupRepository)
    : IRequestHandler<CreateEventCommand, CreateEventResult>
{
    /// <inheritdoc />
    public async Task<CreateEventResult> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (request.EventGroupId is { } eventGroupId)
        {
            var group = await eventGroupRepository.GetByIdAsync(eventGroupId, cancellationToken);
            if (group is null || group.TenantId != request.TenantId)
            {
                return new CreateEventResult(CreateEventOutcome.EventGroupNotFound, null);
            }

            if ((group.StartsAt is not null && request.StartsAt < group.StartsAt)
                || (group.EndsAt is not null && request.EndsAt > group.EndsAt))
            {
                return new CreateEventResult(CreateEventOutcome.OutsideEventGroupRange, null);
            }

            // Legs are compared on their whole run — first performance to last — because that is
            // what the tour advertises. Two legs of one tour cannot be playing at the same time,
            // however many nights each of them lasts.
            var siblingLegs = await repository.ListLegsForEventGroupAsync(eventGroupId, cancellationToken);
            var overlaps = siblingLegs.Any(leg =>
                leg.FirstSessionStartsAt < request.EndsAt && request.StartsAt < leg.LastSessionEndsAt);
            if (overlaps)
            {
                return new CreateEventResult(CreateEventOutcome.OverlapsExistingLeg, null);
            }
        }

        var @event = Event.Create(
            request.TenantId,
            request.Title,
            await DeriveSlugAsync(request, cancellationToken),
            request.Currency,
            request.StartsAt,
            request.EndsAt,
            request.DoorsOpenAt,
            request.BookingEndsAt,
            request.EventGroupId,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.OnSaleAt,
            request.TaxRatePercent,
            request.TaxLabel,
            request.BookingFeePerTicketMinor);

        repository.Add(@event);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateEventResult(CreateEventOutcome.Created, @event.Id);
    }

    /// <summary>
    /// Picks the event's slug: the organizer's own if they supplied one, otherwise derived from the
    /// title, with a numeric suffix either way if it collides.
    /// </summary>
    /// <remarks>
    /// The uniqueness check is read-then-write and therefore racy — two events created in the same
    /// instant with the same title can both see the stem free. That is why the column also carries a
    /// unique index: the loser gets a constraint violation rather than a duplicate URL. Retrying
    /// here would trade a rare 500 for a rarer one and is not worth the complexity at this volume.
    /// </remarks>
    private async Task<string> DeriveSlugAsync(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var basis = string.IsNullOrWhiteSpace(request.Slug) ? request.Title : request.Slug;
        var stem = EventSlug.Basis(basis);
        var taken = await repository.ListSlugsForStemAsync(stem, cancellationToken);

        return EventSlug.From(basis, taken);
    }
}
