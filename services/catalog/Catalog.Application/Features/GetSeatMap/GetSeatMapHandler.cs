namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Handles <see cref="GetSeatMapQuery"/>, mapping the seat map to a read model.</summary>
/// <param name="eventRepository">The event repository, used to enforce the same visibility rule as <c>GetEvent</c>.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
internal sealed class GetSeatMapHandler(IEventRepository eventRepository, ISeatMapRepository seatMapRepository)
    : IRequestHandler<GetSeatMapQuery, SeatMapResponse?>
{
    /// <inheritdoc />
    public async Task<SeatMapResponse?> Handle(GetSeatMapQuery request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || !@event.IsVisibleTo(request.CallerTenantId))
        {
            return null;
        }

        var seatMap = await seatMapRepository.GetByEventIdAsync(request.EventId, cancellationToken);
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
