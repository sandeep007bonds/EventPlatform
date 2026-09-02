namespace Venues.Application.Features.GetVenue;

/// <summary>Handles <see cref="GetVenueQuery"/>.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class GetVenueHandler(IVenueRepository repository)
    : IRequestHandler<GetVenueQuery, VenueResponse?>
{
    /// <inheritdoc />
    public async Task<VenueResponse?> Handle(GetVenueQuery request, CancellationToken cancellationToken)
    {
        var venue = await repository.GetByIdAsync(request.VenueId, cancellationToken);

        return venue is null || venue.TenantId != request.TenantId
            ? null
            : venue.ToResponse();
    }
}
