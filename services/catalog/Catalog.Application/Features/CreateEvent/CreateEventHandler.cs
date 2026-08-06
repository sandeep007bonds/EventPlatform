namespace Catalog.Application.Features.CreateEvent;

/// <summary>Handles <see cref="CreateEventCommand"/> by creating and persisting a draft event.</summary>
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

            var siblingLegs = await repository.ListLegsForEventGroupAsync(eventGroupId, cancellationToken);
            var overlaps = siblingLegs.Any(leg => leg.StartsAt < request.EndsAt && request.StartsAt < leg.EndsAt);
            if (overlaps)
            {
                return new CreateEventResult(CreateEventOutcome.OverlapsExistingLeg, null);
            }
        }

        var @event = Event.Create(
            request.TenantId,
            request.Title,
            request.StartsAt,
            request.EndsAt,
            request.Currency,
            request.LocationName,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.EventGroupId,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue);

        repository.Add(@event);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateEventResult(CreateEventOutcome.Created, @event.Id);
    }
}
