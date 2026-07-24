namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Read model for a single seat.</summary>
/// <param name="Id">Stable seat id (shared across services).</param>
/// <param name="Section">Section name.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceAmount">Seat price in the event's currency.</param>
/// <param name="Row">Row label.</param>
/// <param name="Number">Seat number within the row.</param>
/// <param name="Label">Human-readable seat label.</param>
public sealed record SeatResponse(
    Guid Id,
    string Section,
    string PriceTier,
    decimal PriceAmount,
    string Row,
    int Number,
    string Label);
