namespace Venues.Application.Features.AddVenueFacility;

/// <summary>
/// Handles <see cref="AddVenueFacilityCommand"/>, returning the new facility's id, or
/// <see langword="null"/> when the venue does not exist or belongs to another tenant.
/// </summary>
/// <param name="repository">The venue repository.</param>
internal sealed class AddVenueFacilityHandler(IVenueRepository repository)
    : IRequestHandler<AddVenueFacilityCommand, Guid?>
{
    /// <inheritdoc />
    public async Task<Guid?> Handle(AddVenueFacilityCommand request, CancellationToken cancellationToken)
    {
        var venue = await repository.GetTrackedByIdAsync(request.VenueId, cancellationToken);
        if (venue is null || venue.TenantId != request.TenantId)
        {
            return null;
        }

        var facility = venue.AddFacility(request.Name, request.Description);
        await repository.SaveChangesAsync(cancellationToken);

        return facility.Id;
    }
}
