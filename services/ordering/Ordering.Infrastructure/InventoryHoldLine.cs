namespace Ordering.Infrastructure;

/// <summary>A held seat as returned by the Inventory hold endpoint.</summary>
/// <param name="InventoryItemId">The inventory-item id.</param>
/// <param name="SeatId">The Catalog seat id.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceMinor">Price in minor currency units.</param>
internal sealed record InventoryHoldLine(Guid InventoryItemId, Guid SeatId, string PriceTier, long PriceMinor);
