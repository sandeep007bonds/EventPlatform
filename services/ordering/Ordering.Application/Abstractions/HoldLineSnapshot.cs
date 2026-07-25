namespace Ordering.Application.Abstractions;

/// <summary>A held seat and its price, as read from the Inventory service.</summary>
/// <param name="InventoryItemId">The inventory-item id.</param>
/// <param name="SeatId">The Catalog seat id.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceMinor">Price in minor currency units.</param>
public sealed record HoldLineSnapshot(Guid InventoryItemId, Guid SeatId, string PriceTier, long PriceMinor);
