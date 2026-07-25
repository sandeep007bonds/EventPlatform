namespace Payments.Domain;

/// <summary>Lifecycle state of a payment.</summary>
public enum PaymentStatus
{
    /// <summary>Created; charge not yet attempted or in flight.</summary>
    Initiated,

    /// <summary>Funds captured.</summary>
    Captured,

    /// <summary>The charge failed.</summary>
    Failed,

    /// <summary>A captured charge that was refunded.</summary>
    Refunded,
}
