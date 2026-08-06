namespace Inventory.Application.Holds;

/// <summary>Result of attempting to release a converted hold's sold seats/quantities back to available.</summary>
public enum CancelSoldOutcome
{
    /// <summary>The hold's sold seats/quantities were released (or already were — idempotent).</summary>
    Cancelled,

    /// <summary>No matching hold exists.</summary>
    NotFound,

    /// <summary>The hold is not converted for the given order (never sold, or sold for a different order).</summary>
    NotConverted,

    /// <summary>The release lost a concurrent race.</summary>
    Conflict,
}
