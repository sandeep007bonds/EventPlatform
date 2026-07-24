namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>
/// One section of a seat-map definition; expands to <c>Rows × SeatsPerRow</c> seats.
/// </summary>
/// <param name="Name">Section name; must be unique within the map.</param>
/// <param name="PriceTier">Price tier name for the section.</param>
/// <param name="PriceAmount">Seat price (non-negative) in the event's currency.</param>
/// <param name="Rows">Number of rows (positive).</param>
/// <param name="SeatsPerRow">Seats per row (positive).</param>
public sealed record SeatMapSectionInput(
    string Name,
    string PriceTier,
    decimal PriceAmount,
    int Rows,
    int SeatsPerRow);
