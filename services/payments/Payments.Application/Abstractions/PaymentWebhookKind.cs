namespace Payments.Application.Abstractions;

/// <summary>The provider-neutral meaning of a payment webhook notification.</summary>
public enum PaymentWebhookKind
{
    /// <summary>The provider confirmed the charge was captured.</summary>
    Captured,

    /// <summary>The provider reported the charge failed.</summary>
    Failed,

    /// <summary>The provider confirmed the charge was refunded.</summary>
    Refunded,
}
