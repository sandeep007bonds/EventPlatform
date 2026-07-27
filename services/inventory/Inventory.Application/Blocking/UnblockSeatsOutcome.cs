namespace Inventory.Application.Blocking;

/// <summary>Result of an unblock-seats attempt.</summary>
public enum UnblockSeatsOutcome
{
    /// <summary>All requested seats are now available again.</summary>
    Unblocked,

    /// <summary>One or more requested seats do not exist for this event.</summary>
    SeatNotFound,

    /// <summary>One or more requested seats are not currently blocked.</summary>
    Conflict,
}
