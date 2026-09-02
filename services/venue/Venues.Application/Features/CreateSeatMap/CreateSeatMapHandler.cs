namespace Venues.Application.Features.CreateSeatMap;

/// <summary>
/// Handles <see cref="CreateSeatMapCommand"/>, returning <see langword="null"/> when the venue does
/// not exist or belongs to another tenant.
/// </summary>
/// <param name="venues">The venue repository.</param>
/// <param name="seatMaps">The seat-map repository.</param>
internal sealed class CreateSeatMapHandler(IVenueRepository venues, ISeatMapRepository seatMaps)
    : IRequestHandler<CreateSeatMapCommand, SeatMapResponse?>
{
    /// <inheritdoc />
    public async Task<SeatMapResponse?> Handle(CreateSeatMapCommand request, CancellationToken cancellationToken)
    {
        var venue = await venues.GetByIdAsync(request.VenueId, cancellationToken);
        if (venue is null || venue.TenantId != request.TenantId)
        {
            return null;
        }

        var seatMap = SeatMap.Create(venue.Id, venue.TenantId, request.Name);

        seatMaps.Add(seatMap);
        await seatMaps.SaveChangesAsync(cancellationToken);

        // Create always opens version 1 as a draft, so there is one here by construction.
        return seatMap.ToResponse(seatMap.Draft!);
    }
}
