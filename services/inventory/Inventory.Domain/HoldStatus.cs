namespace Inventory.Domain;

/// <summary>Lifecycle state of a seat hold.</summary>
public enum HoldStatus
{
    /// <summary>Active and within its TTL.</summary>
    Active,

    /// <summary>Converted to a sale (checkout completed).</summary>
    Converted,

    /// <summary>Released — expired by the reaper or cancelled by the buyer.</summary>
    Released,
}
