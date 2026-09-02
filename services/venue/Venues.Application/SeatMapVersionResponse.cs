namespace Venues.Application;

/// <summary>One version of a seat map, with its full layout.</summary>
/// <param name="Id">Version id.</param>
/// <param name="VersionNumber">Version number.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="PublishedAt">When this version was published, if it has been.</param>
/// <param name="Capacity">Sellable seats plus admission-area capacity.</param>
/// <param name="Sections">Reserved-seating sections.</param>
/// <param name="AdmissionAreas">Unreserved capacity areas.</param>
/// <param name="Elements">The graphical layer.</param>
public sealed record SeatMapVersionResponse(
    Guid Id,
    int VersionNumber,
    string Status,
    DateTimeOffset? PublishedAt,
    int Capacity,
    IReadOnlyList<VenueSectionResponse> Sections,
    IReadOnlyList<AdmissionAreaResponse> AdmissionAreas,
    IReadOnlyList<SeatMapElementResponse> Elements);
