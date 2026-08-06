namespace Catalog.Application.Features.UpdateSeatMapSection;

/// <summary>Result of attempting to replace a section of an event's existing seat map.</summary>
public enum UpdateSeatMapSectionOutcome
{
    /// <summary>The section was replaced.</summary>
    Updated,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    EventNotFound,

    /// <summary>The event is no longer a draft, so its seat map can no longer be changed.</summary>
    EventNotDraft,

    /// <summary>No seat map exists yet for this event.</summary>
    SeatMapNotFound,

    /// <summary>No section with <c>CurrentSectionName</c> exists in the seat map.</summary>
    SectionNotFound,

    /// <summary>The new section name collides with a different section already in the seat map.</summary>
    DuplicateSectionName,

    /// <summary>The new section references an entry gate that doesn't exist for this event.</summary>
    EntryGateNotFound,
}
