namespace Venues.Api.Endpoints;

/// <summary>One row in a submitted layout.</summary>
/// <param name="Label">Row label, unique within its section.</param>
/// <param name="DisplayOrder">Front-to-back ordering within the section.</param>
/// <param name="Seats">The row's seats, in order. Absent is treated as empty.</param>
public sealed record SeatMapRowRequest(
    string Label,
    int DisplayOrder,
    IReadOnlyList<SeatMapSeatRequest>? Seats);
