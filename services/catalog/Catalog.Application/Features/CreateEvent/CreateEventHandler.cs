namespace Catalog.Application.Features.CreateEvent;

/// <summary>Handles <see cref="CreateEventCommand"/> by creating and persisting a draft event.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class CreateEventHandler(IEventRepository repository)
    : IRequestHandler<CreateEventCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = Event.Create(
            request.TenantId,
            request.Title,
            request.StartsAt,
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
            request.EventGroupId);

        repository.Add(@event);
        await repository.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
