namespace Venues.Application.Features.ListSeatMaps;

/// <summary>Handles <see cref="ListSeatMapsQuery"/>.</summary>
/// <param name="repository">The seat-map repository.</param>
internal sealed class ListSeatMapsHandler(ISeatMapRepository repository)
    : IRequestHandler<ListSeatMapsQuery, IReadOnlyList<SeatMapSummaryResponse>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatMapSummaryResponse>> Handle(
        ListSeatMapsQuery request,
        CancellationToken cancellationToken)
    {
        var seatMaps = await repository.ListForVenueAsync(request.VenueId, cancellationToken);

        return seatMaps
            .Where(map => map.TenantId == request.TenantId)
            .Select(map => map.ToSummary())
            .ToList();
    }
}
