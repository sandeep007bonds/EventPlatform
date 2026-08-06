namespace Catalog.Application.Features.AddSeatMapSections;

/// <summary>Result of attempting to add sections to an event's existing seat map.</summary>
public enum AddSeatMapSectionsOutcome
{
    /// <summary>The sections were added.</summary>
    Added,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    EventNotFound,

    /// <summary>The event is no longer a draft, so its seat map can no longer be changed.</summary>
    EventNotDraft,

    /// <summary>No seat map exists yet for this event — define one first.</summary>
    SeatMapNotFound,

    /// <summary>A section name collides with one already in the seat map.</summary>
    DuplicateSectionName,

    /// <summary>A section references an entry gate that doesn't exist for this event.</summary>
    EntryGateNotFound,
}
