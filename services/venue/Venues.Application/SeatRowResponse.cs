namespace Venues.Application;

/// <summary>A row of seats as returned by the API.</summary>
/// <param name="Id">Row id.</param>
/// <param name="Label">Row label, unique within the section.</param>
/// <param name="DisplayOrder">Front-to-back ordering within the section.</param>
/// <param name="Seats">The row's seats.</param>
public sealed record SeatRowResponse(
    Guid Id,
    string Label,
    int DisplayOrder,
    IReadOnlyList<SeatResponse> Seats);
