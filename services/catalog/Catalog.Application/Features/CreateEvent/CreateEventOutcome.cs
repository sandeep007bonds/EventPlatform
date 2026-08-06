namespace Catalog.Application.Features.CreateEvent;

/// <summary>Result of attempting to create a draft event.</summary>
public enum CreateEventOutcome
{
    /// <summary>The event was created.</summary>
    Created,

    /// <summary>The supplied <c>EventGroupId</c> does not exist, or belongs to another tenant.</summary>
    EventGroupNotFound,

    /// <summary>The event's date range falls outside its tour's advertised date range.</summary>
    OutsideEventGroupRange,

    /// <summary>The event's date range overlaps another leg of the same tour.</summary>
    OverlapsExistingLeg,
}
