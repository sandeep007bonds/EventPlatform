namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Read model returned for an event's seat map. This is the hand-off Inventory reads.</summary>
/// <param name="EventId">The event the seat map belongs to.</param>
/// <param name="Name">Seat-map name.</param>
/// <param name="Capacity">Total sellable capacity — reserved seats plus general-admission capacity.</param>
/// <param name="Seats">The reserved seats, ordered by generation.</param>
/// <param name="GeneralAdmissionSections">The general-admission sections (capacity pools, no individual seats).</param>
public sealed record SeatMapResponse(
    Guid EventId,
    string Name,
    int Capacity,
    IReadOnlyList<SeatResponse> Seats,
    IReadOnlyList<GeneralAdmissionSectionResponse> GeneralAdmissionSections);
