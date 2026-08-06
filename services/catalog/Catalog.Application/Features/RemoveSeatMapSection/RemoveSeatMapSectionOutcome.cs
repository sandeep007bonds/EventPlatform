namespace Catalog.Application.Features.RemoveSeatMapSection;

/// <summary>Result of attempting to remove a section from an event's existing seat map.</summary>
public enum RemoveSeatMapSectionOutcome
{
    /// <summary>The section was removed.</summary>
    Removed,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    EventNotFound,

    /// <summary>The event is no longer a draft, so its seat map can no longer be changed.</summary>
    EventNotDraft,

    /// <summary>No seat map exists yet for this event.</summary>
    SeatMapNotFound,

    /// <summary>No section with that name exists in the seat map.</summary>
    SectionNotFound,
}
