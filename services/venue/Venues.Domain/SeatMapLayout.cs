namespace Venues.Domain;

/// <summary>A complete seat-map layout as the designer submits it: everything a version contains.</summary>
/// <remarks>
/// Whole-layout replacement rather than a stream of edits. A graphical editor already holds the
/// entire plan in memory and knows nothing about which of a hundred nudges the server has seen, so
/// a patch protocol would mean inventing an operation vocabulary, ordering it, and reconciling
/// conflicts — for a draft only one person edits at a time. Sending the plan is simpler and cannot
/// half-apply.
/// </remarks>
/// <param name="Sections">Reserved-seating sections.</param>
/// <param name="AdmissionAreas">Unreserved capacity areas.</param>
/// <param name="Elements">The graphical layer.</param>
public sealed record SeatMapLayout(
    IReadOnlyList<SectionDraft> Sections,
    IReadOnlyList<AdmissionAreaDraft> AdmissionAreas,
    IReadOnlyList<SeatMapElementDraft> Elements);
