namespace Inventory.Application.Blocking;

/// <summary>Result of a block-seats attempt.</summary>
public enum BlockSeatsOutcome
{
    /// <summary>All requested seats are now blocked.</summary>
    Blocked,

    /// <summary>One or more requested seats do not exist for this event.</summary>
    SeatNotFound,

    /// <summary>One or more requested seats are not available (held, sold, or already blocked).</summary>
    Conflict,
}
