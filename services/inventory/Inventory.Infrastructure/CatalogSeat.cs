namespace Inventory.Infrastructure;

/// <summary>A seat in the Catalog seat-map response.</summary>
/// <param name="Id">Seat id.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceAmount">Seat price in the event's currency.</param>
internal sealed record CatalogSeat(Guid Id, string PriceTier, decimal PriceAmount);
