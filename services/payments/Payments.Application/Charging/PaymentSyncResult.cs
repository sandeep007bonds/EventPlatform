namespace Payments.Application.Charging;

/// <summary>Outcome of re-reading a payment's state from the provider.</summary>
public enum PaymentSyncResult
{
    /// <summary>No payment exists for that order yet.</summary>
    NotFound,

    /// <summary>Still in flight — the buyer hasn't finished authenticating.</summary>
    Pending,

    /// <summary>Captured: the money moved.</summary>
    Captured,

    /// <summary>Failed or was cancelled at the provider.</summary>
    Failed,
}
