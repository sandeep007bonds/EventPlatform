namespace Inventory.Infrastructure;

/// <summary>One version of a Venue seat map, as far as Inventory reads it.</summary>
/// <param name="Sections">Reserved-seating sections, each with its rows and seats.</param>
/// <param name="AdmissionAreas">Unreserved capacity areas.</param>
internal sealed record VenueSeatMapVersion(
    IReadOnlyList<VenueSection> Sections,
    IReadOnlyList<VenueAdmissionArea> AdmissionAreas);
