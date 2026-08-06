namespace Ticketing.Application.Abstractions;

/// <summary>
/// Which entry gate (if any) each reserved seat or general-admission section of an event's
/// Catalog seat map is restricted to. Resolved once per event by
/// <c>EventScanContextProvisioningService</c>, never queried live at scan time.
/// </summary>
/// <param name="EntryGateIdBySeatId">Each reserved seat's restricted entry gate, keyed by seat id (only entries with a restriction).</param>
/// <param name="EntryGateIdByCatalogSectionId">Each general-admission section's restricted entry gate, keyed by the Catalog section id (only entries with a restriction).</param>
public sealed record CatalogGateMap(
    IReadOnlyDictionary<Guid, Guid> EntryGateIdBySeatId,
    IReadOnlyDictionary<Guid, Guid> EntryGateIdByCatalogSectionId);
