namespace Venues.Application.Features.PublishSeatMap;

/// <summary>What happened when a seat-map draft was published.</summary>
public enum PublishSeatMapOutcome
{
    /// <summary>The draft was frozen and is now live.</summary>
    Published = 0,

    /// <summary>No such seat map, or it belongs to another tenant.</summary>
    NotFound = 1,

    /// <summary>There is no open draft to publish.</summary>
    NoOpenDraft = 2,

    /// <summary>The layout did not validate. Every reason is listed, not just the first.</summary>
    Invalid = 3,
}
