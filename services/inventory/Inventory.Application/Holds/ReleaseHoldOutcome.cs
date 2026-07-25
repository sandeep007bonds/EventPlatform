namespace Inventory.Application.Holds;

/// <summary>Result of attempting to release a hold.</summary>
public enum ReleaseHoldOutcome
{
    /// <summary>The hold was released.</summary>
    Released,

    /// <summary>No matching hold exists.</summary>
    NotFound,

    /// <summary>The hold belongs to another user.</summary>
    Forbidden,

    /// <summary>The hold is not active (already converted or released).</summary>
    NotActive,

    /// <summary>The hold could not be released due to a concurrent change.</summary>
    Conflict,
}
