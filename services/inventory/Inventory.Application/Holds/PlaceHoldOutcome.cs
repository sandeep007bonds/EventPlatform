namespace Inventory.Application.Holds;

/// <summary>Result of attempting to place a seat hold.</summary>
public enum PlaceHoldOutcome
{
    /// <summary>All seats were held.</summary>
    Held,

    /// <summary>One or more requested seats do not exist for the event.</summary>
    SeatNotFound,

    /// <summary>One or more seats are no longer available (lost the race).</summary>
    Conflict,
}
