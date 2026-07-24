namespace Catalog.Application.Features.PublishEvent;

/// <summary>Result of attempting to publish an event.</summary>
public enum PublishEventOutcome
{
    /// <summary>The event was published and its <c>EventPublished</c> event enqueued.</summary>
    Published,

    /// <summary>No matching event exists.</summary>
    NotFound,

    /// <summary>The event has no seat map, so there is nothing to sell.</summary>
    NoSeatMap,

    /// <summary>The event is not a draft, so it cannot be published.</summary>
    NotDraft,
}
