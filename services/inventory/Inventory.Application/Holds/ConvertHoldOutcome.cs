namespace Inventory.Application.Holds;

/// <summary>Result of attempting to convert a hold to a sale.</summary>
public enum ConvertHoldOutcome
{
    /// <summary>The hold was converted (or was already converted for the same order — idempotent).</summary>
    Converted,

    /// <summary>No matching hold exists.</summary>
    NotFound,

    /// <summary>The hold is not active (already released, or converted for a different order).</summary>
    NotActive,

    /// <summary>The hold has expired and can no longer be sold.</summary>
    Expired,

    /// <summary>The conversion lost a concurrent race.</summary>
    Conflict,
}
