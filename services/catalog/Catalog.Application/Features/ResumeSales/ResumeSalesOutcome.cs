namespace Catalog.Application.Features.ResumeSales;

/// <summary>Result of attempting to resume sales for an event.</summary>
public enum ResumeSalesOutcome
{
    /// <summary>Sales were resumed and the <c>EventSalesResumed</c> event enqueued.</summary>
    Resumed,

    /// <summary>No matching event exists.</summary>
    NotFound,

    /// <summary>The event is not published, so its sales cannot be resumed.</summary>
    NotPublished,

    /// <summary>Sales are not paused for this event.</summary>
    NotPaused,
}
