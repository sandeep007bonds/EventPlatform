namespace Catalog.Application.Features.CreateVenue;

/// <summary>Handles <see cref="CreateVenueCommand"/> by creating and persisting a venue.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class CreateVenueHandler(IVenueRepository repository)
    : IRequestHandler<CreateVenueCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CreateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = Venue.Create(
            request.TenantId,
            request.Name,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.Capacity);

        repository.Add(venue);
        await repository.SaveChangesAsync(cancellationToken);

        return venue.Id;
    }
}
