namespace Catalog.Domain;

/// <summary>Lifecycle state of a catalog event.</summary>
public enum EventStatus
{
    /// <summary>Being configured; not visible to buyers.</summary>
    Draft,

    /// <summary>Published; inventory generated; visible in the storefront.</summary>
    Published,

    /// <summary>Currently on sale.</summary>
    OnSale,

    /// <summary>All inventory has been sold.</summary>
    SoldOut,

    /// <summary>Cancelled; refunds issued.</summary>
    Cancelled,

    /// <summary>The event has taken place.</summary>
    Completed,
}
