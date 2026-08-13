namespace Payments.Application.Abstractions;

/// <summary>The provider's current view of a payment, as read back on demand.</summary>
public enum GatewayPaymentStatus
{
    /// <summary>Still in flight — the buyer hasn't finished authenticating, or capture is pending.</summary>
    Pending,

    /// <summary>Captured: the money moved.</summary>
    Captured,

    /// <summary>Failed or was cancelled at the provider.</summary>
    Failed,
}
