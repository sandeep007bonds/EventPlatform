namespace Catalog.Application.Features.UpdateEventPresentation;

/// <summary>Result of attempting to update an event's presentation.</summary>
/// <remarks>
/// There is deliberately no <c>NotDraft</c> member: presentation is editable for the life of the
/// event, which is the whole point of splitting it out of the selling rules.
/// </remarks>
public enum UpdateEventPresentationOutcome
{
    /// <summary>The event's presentation was updated and its <c>EventUpdated</c> event enqueued.</summary>
    Updated,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    NotFound,
}
