namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>Result of attempting to update an event's details.</summary>
public enum UpdateEventDetailsOutcome
{
    /// <summary>The event's details were updated and its <c>EventUpdated</c> event enqueued.</summary>
    Updated,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    NotFound,

    /// <summary>The event is not a draft, so its details can no longer be changed.</summary>
    NotDraft,

    /// <summary>The booking cutoff would be later than the event's own start time.</summary>
    BookingCutoffAfterStart,

    /// <summary>The new end time would fall outside the owning tour's advertised date range.</summary>
    OutsideEventGroupRange,

    /// <summary>The new date range would overlap another leg of the same tour.</summary>
    OverlapsExistingLeg,
}
