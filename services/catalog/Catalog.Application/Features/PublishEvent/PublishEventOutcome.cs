namespace Catalog.Application.Features.PublishEvent;

/// <summary>Result of attempting to publish an event.</summary>
public enum PublishEventOutcome
{
    /// <summary>The event was published, with one <c>EventSessionPublished</c> per performance.</summary>
    Published,

    /// <summary>No matching event exists.</summary>
    NotFound,

    /// <summary>The event is not a draft, so it cannot be published.</summary>
    NotDraft,

    /// <summary>
    /// No performance is ready to sell — see the accompanying problems for which and why.
    /// </summary>
    NoSellablePerformance,
}
