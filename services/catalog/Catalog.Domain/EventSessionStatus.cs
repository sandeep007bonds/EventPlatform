namespace Catalog.Domain;

/// <summary>Lifecycle state of one <see cref="EventSession"/>.</summary>
/// <remarks>
/// A session has its own status rather than inheriting the event's, because adding a late show to a
/// run that is already on sale is ordinary work: the new performance is a draft while its seat map
/// and pricing are set up, and goes live on its own without republishing the event.
/// </remarks>
public enum EventSessionStatus
{
    /// <summary>Being set up. Times, seat map and allocations can still change.</summary>
    Draft = 0,

    /// <summary>Live. Inventory has been provisioned and tickets can be sold.</summary>
    Published = 1,

    /// <summary>
    /// Called off. Kept, never deleted — orders and tickets reference it and their history has to
    /// keep making sense.
    /// </summary>
    Cancelled = 2,
}
