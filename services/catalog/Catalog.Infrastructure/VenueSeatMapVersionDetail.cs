namespace Catalog.Infrastructure;

/// <summary>One version of a Venue seat map, as far as Catalog reads it.</summary>
/// <param name="Id">Version id.</param>
/// <param name="VersionNumber">Version number.</param>
/// <param name="Status">Lifecycle status name — <c>Draft</c>, <c>Published</c> or <c>Superseded</c>.</param>
/// <param name="Capacity">Sellable seats plus admission-area capacity.</param>
/// <param name="Sections">Reserved-seating sections; only their codes are read.</param>
/// <param name="AdmissionAreas">Unreserved capacity areas; only their codes are read.</param>
internal sealed record VenueSeatMapVersionDetail(
    Guid Id,
    int VersionNumber,
    string Status,
    int Capacity,
    IReadOnlyList<VenueBlock> Sections,
    IReadOnlyList<VenueBlock> AdmissionAreas);
