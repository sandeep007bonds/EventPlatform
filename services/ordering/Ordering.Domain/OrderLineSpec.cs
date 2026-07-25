namespace Ordering.Domain;

/// <summary>Input describing one line of a new order (a held seat and its price).</summary>
/// <param name="InventoryItemId">The inventory-item id.</param>
/// <param name="SeatId">The Catalog seat id.</param>
/// <param name="PriceMinor">Price in minor currency units.</param>
public sealed record OrderLineSpec(Guid InventoryItemId, Guid SeatId, long PriceMinor);
