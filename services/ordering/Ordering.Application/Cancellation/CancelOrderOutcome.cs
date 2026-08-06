namespace Ordering.Application.Cancellation;

/// <summary>Result of a buyer-initiated order cancellation attempt.</summary>
public enum CancelOrderOutcome
{
    /// <summary>Tickets voided, inventory released, payment refunded — the order is refunded.</summary>
    Cancelled,

    /// <summary>The order does not exist.</summary>
    OrderNotFound,

    /// <summary>The order belongs to another buyer.</summary>
    Forbidden,

    /// <summary>The order is not confirmed (still in progress, already refunded, or failed).</summary>
    NotConfirmed,

    /// <summary>At least one ticket for the order has already been checked in — nothing was cancelled.</summary>
    TicketAlreadyCheckedIn,

    /// <summary>A step of the cancellation could not complete.</summary>
    Failed,
}
