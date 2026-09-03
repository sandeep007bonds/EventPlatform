namespace Catalog.Application.Features.ChangeEventSales;

/// <summary>What happened when sales were paused or resumed across an event.</summary>
public enum ChangeEventSalesOutcome
{
    /// <summary>Every performance was switched, and one integration event enqueued for each.</summary>
    Changed,

    /// <summary>No matching event exists.</summary>
    NotFound,

    /// <summary>The event is not published, so its sales cannot be paused or resumed.</summary>
    NotPublished,
}
