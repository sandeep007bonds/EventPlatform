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

    /// <summary>This request would push the buyer's total commitment for the event past its per-buyer ticket limit.</summary>
    BuyerLimitExceeded,

    /// <summary>The event's enforced on-sale start has not yet arrived; no new holds are accepted.</summary>
    OnSaleNotStarted,

    /// <summary>The event has not been provisioned in Inventory yet (no <c>EventPublished</c> processed for it).</summary>
    EventNotFound,

    /// <summary>The event requires Queue admission and the request carried no valid, unexpired admission token.</summary>
    QueueAdmissionRequired,

    /// <summary>An organizer has manually paused sales for this event; no new holds are accepted.</summary>
    SalesPaused,
}
