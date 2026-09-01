namespace Catalog.Application.Features.ChangeEventSlug;

/// <summary>Result of attempting to change an event's slug.</summary>
public enum ChangeEventSlugOutcome
{
    /// <summary>The slug was changed.</summary>
    Changed,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    NotFound,

    /// <summary>The event is published, so its URL is fixed.</summary>
    NotDraft,

    /// <summary>Another event already uses that slug.</summary>
    SlugTaken,
}
