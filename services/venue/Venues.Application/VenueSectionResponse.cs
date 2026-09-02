namespace Venues.Application;

/// <summary>A reserved-seating section as returned by the API.</summary>
/// <param name="Id">Section id.</param>
/// <param name="Code">Short stable code, unique within the version.</param>
/// <param name="Name">Display name.</param>
/// <param name="DisplayOrder">Ordering when sections are listed.</param>
/// <param name="GateId">The gate this section is entered through, if any.</param>
/// <param name="SellableSeatCount">Seats that can be sold.</param>
/// <param name="Rows">The section's rows.</param>
public sealed record VenueSectionResponse(
    Guid Id,
    string Code,
    string Name,
    int DisplayOrder,
    Guid? GateId,
    int SellableSeatCount,
    IReadOnlyList<SeatRowResponse> Rows);
