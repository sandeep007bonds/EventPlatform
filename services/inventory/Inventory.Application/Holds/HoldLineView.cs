namespace Inventory.Application.Holds;

/// <summary>A single held seat, with its price, for hold validation and order pricing.</summary>
/// <param name="InventoryItemId">The inventory-item id.</param>
/// <param name="SeatId">The Catalog seat id.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceMinor">Price in minor currency units.</param>
public sealed record HoldLineView(Guid InventoryItemId, Guid SeatId, string PriceTier, long PriceMinor);
