namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Read model returned for an event's seat map. This is the hand-off Inventory reads.</summary>
/// <param name="EventId">The event the seat map belongs to.</param>
/// <param name="Name">Seat-map name.</param>
/// <param name="Capacity">Total number of seats.</param>
/// <param name="Seats">The seats, ordered by generation.</param>
public sealed record SeatMapResponse(
    Guid EventId,
    string Name,
    int Capacity,
    IReadOnlyList<SeatResponse> Seats);
