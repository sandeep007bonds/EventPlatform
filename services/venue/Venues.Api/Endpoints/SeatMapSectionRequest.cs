namespace Venues.Api.Endpoints;

/// <summary>One reserved-seating section in a submitted layout.</summary>
/// <param name="Code">Short stable code, unique within the version across sections and areas.</param>
/// <param name="Name">Display name.</param>
/// <param name="DisplayOrder">Ordering when sections are listed.</param>
/// <param name="GateId">The gate this section is entered through, or <see langword="null"/> for any.</param>
/// <param name="Rows">The section's rows, in order. Absent is treated as empty.</param>
public sealed record SeatMapSectionRequest(
    string Code,
    string Name,
    int DisplayOrder,
    Guid? GateId,
    IReadOnlyList<SeatMapRowRequest>? Rows);
