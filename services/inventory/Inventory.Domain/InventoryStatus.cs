namespace Inventory.Domain;

/// <summary>Lifecycle state of an inventory item (one sellable seat).</summary>
public enum InventoryStatus
{
    /// <summary>Available to be held or sold.</summary>
    Available,

    /// <summary>Held for a buyer (subject to a TTL).</summary>
    Held,

    /// <summary>Sold; permanently unavailable.</summary>
    Sold,

    /// <summary>Blocked by the organizer (e.g. kill, restricted view); not sellable.</summary>
    Blocked,
}
