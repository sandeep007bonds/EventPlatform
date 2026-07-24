namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Handles <see cref="GetSeatMapQuery"/>, mapping the seat map to a read model.</summary>
/// <param name="repository">The seat-map repository.</param>
internal sealed class GetSeatMapHandler(ISeatMapRepository repository)
    : IRequestHandler<GetSeatMapQuery, SeatMapResponse?>
{
    /// <inheritdoc />
    public async Task<SeatMapResponse?> Handle(GetSeatMapQuery request, CancellationToken cancellationToken)
    {
        var seatMap = await repository.GetByEventIdAsync(request.EventId, cancellationToken);
        if (seatMap is null)
        {
            return null;
        }

        var seats = seatMap.Seats
            .OrderBy(s => s.Id)
            .Select(s => new SeatResponse(s.Id, s.Section, s.PriceTier, s.PriceAmount, s.Row, s.Number, s.Label))
            .ToList();

        return new SeatMapResponse(seatMap.EventId, seatMap.Name, seatMap.Capacity, seats);
    }
}
