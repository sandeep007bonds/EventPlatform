namespace Ticketing.Infrastructure;

/// <summary>Subset of Catalog's seat-map response needed to resolve a ticket's entry-gate restriction.</summary>
/// <param name="Seats">The reserved seats in the map.</param>
/// <param name="GeneralAdmissionSections">The general-admission sections in the map.</param>
internal sealed record CatalogSeatMapForScan(
    IReadOnlyList<CatalogScanSeat> Seats,
    IReadOnlyList<CatalogScanGaSection> GeneralAdmissionSections);
