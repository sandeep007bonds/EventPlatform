namespace Ordering.Domain;

/// <summary>Lifecycle state of a checkout order.</summary>
public enum OrderStatus
{
    /// <summary>Created; not yet charged.</summary>
    Pending,

    /// <summary>Payment in progress.</summary>
    AwaitingPayment,

    /// <summary>Paid and seats sold — the sale is complete.</summary>
    Confirmed,

    /// <summary>Checkout failed; seats released.</summary>
    Failed,

    /// <summary>A confirmed order that was later refunded.</summary>
    Refunded,
}
