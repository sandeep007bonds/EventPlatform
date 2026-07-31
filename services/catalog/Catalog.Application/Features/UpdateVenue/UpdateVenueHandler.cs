namespace Catalog.Application.Features.UpdateVenue;

/// <summary>Handles <see cref="UpdateVenueCommand"/> by updating an existing venue's details.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class UpdateVenueHandler(IVenueRepository repository)
    : IRequestHandler<UpdateVenueCommand, UpdateVenueOutcome>
{
    /// <inheritdoc />
    public async Task<UpdateVenueOutcome> Handle(UpdateVenueCommand request, CancellationToken cancellationToken)
    {
        var venue = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (venue is null || venue.TenantId != request.TenantId)
        {
            return UpdateVenueOutcome.NotFound;
        }

        venue.Update(
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

        await repository.SaveChangesAsync(cancellationToken);
        return UpdateVenueOutcome.Updated;
    }
}
