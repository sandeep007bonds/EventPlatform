namespace Inventory.Infrastructure;

/// <summary>Subset of the Catalog seat-map response needed to provision inventory.</summary>
/// <param name="Seats">The seats in the map.</param>
internal sealed record CatalogSeatMap(IReadOnlyList<CatalogSeat> Seats);
