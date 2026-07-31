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
}
