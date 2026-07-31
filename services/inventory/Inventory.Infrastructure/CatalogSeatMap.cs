namespace Inventory.Infrastructure;

/// <summary>Subset of the Catalog seat-map response needed to provision inventory.</summary>
/// <param name="Seats">The reserved seats in the map.</param>
/// <param name="GeneralAdmissionSections">The general-admission sections in the map.</param>
internal sealed record CatalogSeatMap(IReadOnlyList<CatalogSeat> Seats, IReadOnlyList<CatalogGeneralAdmissionSection> GeneralAdmissionSections);
