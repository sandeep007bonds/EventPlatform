namespace Venues.Application.Features.UpdateVenue;

/// <summary>
/// Handles <see cref="UpdateVenueCommand"/>. A venue belonging to another tenant is reported as
/// not found rather than forbidden — the same opaque-404 pattern the rest of the platform uses, so
/// an id probe cannot confirm a venue exists.
/// </summary>
/// <param name="repository">The venue repository.</param>
internal sealed class UpdateVenueHandler(IVenueRepository repository)
    : IRequestHandler<UpdateVenueCommand, VenueResponse?>
{
    /// <inheritdoc />
    public async Task<VenueResponse?> Handle(UpdateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await repository.GetTrackedByIdAsync(request.VenueId, cancellationToken);
        if (venue is null || venue.TenantId != request.TenantId)
        {
            return null;
        }

        venue.UpdateDetails(
            request.Name,
            request.VenueType,
            new VenueAddress(
                request.Address.AddressLine1,
                request.Address.AddressLine2,
                request.Address.City,
                request.Address.Region,
                request.Address.PostalCode,
                request.Address.Country,
                request.Address.Latitude,
                request.Address.Longitude),
            request.TimeZoneId);

        await repository.SaveChangesAsync(cancellationToken);

        return venue.ToResponse();
    }
}
