namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>Result of attempting to define a seat map for an event.</summary>
public enum DefineSeatMapOutcome
{
    /// <summary>The seat map was created.</summary>
    Created,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    EventNotFound,

    /// <summary>The event is no longer a draft, so its seat map can no longer be defined.</summary>
    EventNotDraft,

    /// <summary>A seat map already exists for the event.</summary>
    AlreadyDefined,
}
