namespace Ticketing.Infrastructure;

/// <summary>A reserved seat's entry-gate restriction from the Catalog seat-map response.</summary>
/// <param name="Id">Seat id.</param>
/// <param name="EntryGateId">The entry gate this seat's section is restricted to, if any.</param>
internal sealed record CatalogScanSeat(Guid Id, Guid? EntryGateId);
