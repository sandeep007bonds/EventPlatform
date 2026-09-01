namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Handles <see cref="GetSeatMapQuery"/>, mapping the seat map to a read model.</summary>
/// <param name="eventRepository">The event repository, used to enforce the same visibility rule as <c>GetEvent</c>.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
/// <param name="ticketTypeRepository">The ticket-type repository — the source of each section's name and price.</param>
internal sealed class GetSeatMapHandler(
    IEventRepository eventRepository,
    ISeatMapRepository seatMapRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<GetSeatMapQuery, SeatMapResponse?>
{
    /// <summary>Minor units per major unit; this response still speaks in major units.</summary>
    private const decimal MinorUnitsPerMajor = 100m;

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

        // Name and price come from the ticket type, never from the copies still held on each seat
        // row. That is the whole point of the type owning them: renaming a tier or repricing it
        // takes effect here immediately, instead of leaving thousands of stale seat rows to be
        // rewritten. The obsolete columns are the fallback only for rows a backfill somehow missed,
        // which should be none.
        var typesById = (await ticketTypeRepository.ListForEventAsync(request.EventId, cancellationToken))
            .ToDictionary(type => type.Id);

        var seats = seatMap.Seats
            .OrderBy(s => s.Id)
            .Select(s => new SeatResponse(
                s.Id,
                s.Section,
                NameOf(typesById, s.TicketTypeId, s.PriceTier),
                PriceOf(typesById, s.TicketTypeId, s.PriceAmount),
                s.Row,
                s.Number,
                s.Label,
                s.EntryGateId))
            .ToList();

        var gaSections = seatMap.GeneralAdmissionSections
            .OrderBy(s => s.Id)
            .Select(s => new GeneralAdmissionSectionResponse(
                s.Id,
                s.SectionName,
                NameOf(typesById, s.TicketTypeId, s.PriceTier),
                PriceOf(typesById, s.TicketTypeId, s.PriceAmount),
                s.Capacity,
                s.EntryGateId))
            .ToList();

        return new SeatMapResponse(seatMap.EventId, seatMap.Name, seatMap.Capacity, seats, gaSections);
    }

    // Concrete Dictionary rather than IReadOnlyDictionary (CA1859): these are private helpers with
    // one caller that builds the dictionary itself, so the interface buys no flexibility and costs
    // an interface dispatch per seat — on a map with tens of thousands of them.
    private static string NameOf(Dictionary<Guid, TicketType> types, Guid ticketTypeId, string fallback) =>
        types.TryGetValue(ticketTypeId, out var type) ? type.Name : fallback;

    private static decimal PriceOf(Dictionary<Guid, TicketType> types, Guid ticketTypeId, decimal fallback) =>
        types.TryGetValue(ticketTypeId, out var type) ? type.PriceMinor / MinorUnitsPerMajor : fallback;
}
