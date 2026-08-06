namespace Ticketing.Infrastructure;

/// <summary>A general-admission section's entry-gate restriction from the Catalog seat-map response.</summary>
/// <param name="Id">Section id (the Catalog section id — what a GA allocation's <c>CatalogSectionId</c> maps to).</param>
/// <param name="EntryGateId">The entry gate this section is restricted to, if any.</param>
internal sealed record CatalogScanGaSection(Guid Id, Guid? EntryGateId);
