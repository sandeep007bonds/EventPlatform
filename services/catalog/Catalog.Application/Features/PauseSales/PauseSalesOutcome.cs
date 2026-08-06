namespace Catalog.Application.Features.PauseSales;

/// <summary>Result of attempting to pause sales for an event.</summary>
public enum PauseSalesOutcome
{
    /// <summary>Sales were paused and the <c>EventSalesPaused</c> event enqueued.</summary>
    Paused,

    /// <summary>No matching event exists.</summary>
    NotFound,

    /// <summary>The event is not published, so its sales cannot be paused.</summary>
    NotPublished,

    /// <summary>Sales are already paused for this event.</summary>
    AlreadyPaused,
}
