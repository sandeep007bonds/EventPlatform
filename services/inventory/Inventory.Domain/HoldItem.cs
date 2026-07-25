namespace Inventory.Domain;

/// <summary>A single inventory item that is part of a <see cref="Hold"/>.</summary>
public sealed class HoldItem
{
    internal HoldItem(Guid holdId, Guid inventoryItemId)
    {
        HoldId = holdId;
        InventoryItemId = inventoryItemId;
    }

    // Parameterless ctor for EF Core materialization.
    private HoldItem()
    {
    }

    /// <summary>The hold this item belongs to.</summary>
    public Guid HoldId { get; private set; }

    /// <summary>The held inventory item.</summary>
    public Guid InventoryItemId { get; private set; }
}
