namespace Venues.Application.Features.CreateVenue;

/// <summary>
/// Handles <see cref="CreateVenueCommand"/> by creating the venue and enqueuing a
/// <see cref="VenueCreated"/> integration event in the same unit of work.
/// </summary>
/// <param name="repository">The venue repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class CreateVenueHandler(IVenueRepository repository, IEventPublisher events)
    : IRequestHandler<CreateVenueCommand, VenueResponse>
{
    /// <inheritdoc />
    public async Task<VenueResponse> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var address = new VenueAddress(
            request.Address.AddressLine1,
            request.Address.AddressLine2,
            request.Address.City,
            request.Address.Region,
            request.Address.PostalCode,
            request.Address.Country,
            request.Address.Latitude,
            request.Address.Longitude);

        var venue = Venue.Create(request.TenantId, request.Name, request.VenueType, address, request.TimeZoneId);

        repository.Add(venue);

        events.Enqueue(new VenueCreated(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            venue.TenantId,
            venue.Id,
            venue.Name,
            venue.Address.City,
            venue.Address.Country));

        await repository.SaveChangesAsync(cancellationToken);

        return venue.ToResponse();
    }
}
