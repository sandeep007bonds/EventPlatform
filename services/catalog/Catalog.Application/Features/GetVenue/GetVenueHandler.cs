namespace Catalog.Application.Features.GetVenue;

/// <summary>Handles <see cref="GetVenueQuery"/>, mapping the aggregate to a read model.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class GetVenueHandler(IVenueRepository repository)
    : IRequestHandler<GetVenueQuery, VenueResponse?>
{
    /// <inheritdoc />
    public async Task<VenueResponse?> Handle(GetVenueQuery request, CancellationToken cancellationToken)
    {
        var venue = await repository.GetByIdAsync(request.Id, cancellationToken);

        return venue is null
            ? null
            : new VenueResponse(
                venue.Id,
                venue.Name,
                venue.AddressLine1,
                venue.AddressLine2,
                venue.City,
                venue.Region,
                venue.PostalCode,
                venue.Country,
                venue.Latitude,
                venue.Longitude,
                venue.Capacity);
    }
}
