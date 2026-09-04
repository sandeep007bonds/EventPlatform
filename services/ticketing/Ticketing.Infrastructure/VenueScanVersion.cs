namespace Ticketing.Infrastructure;

/// <summary>One seat-map version, as far as Ticketing's gate map reads it.</summary>
/// <param name="Sections">Reserved-seating sections, each with its gate and its seats.</param>
/// <param name="AdmissionAreas">Unreserved capacity areas, each with its gate.</param>
internal sealed record VenueScanVersion(
    IReadOnlyList<VenueScanSection> Sections,
    IReadOnlyList<VenueScanArea> AdmissionAreas);
