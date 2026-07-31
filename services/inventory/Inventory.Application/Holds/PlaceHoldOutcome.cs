namespace Inventory.Application.Holds;

/// <summary>Result of attempting to place a seat hold.</summary>
public enum PlaceHoldOutcome
{
    /// <summary>All seats were held.</summary>
    Held,

    /// <summary>One or more requested seats do not exist for the event.</summary>
    SeatNotFound,

    /// <summary>One or more requested general-admission allocations do not exist for the event.</summary>
    AllocationNotFound,

    /// <summary>One or more seats or general-admission allocations are no longer available (lost the race).</summary>
    Conflict,

    /// <summary>The event's enforced booking cutoff has passed; no new holds are accepted.</summary>
    BookingWindowClosed,
}
