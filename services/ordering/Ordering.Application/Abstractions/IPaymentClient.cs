namespace Ordering.Application.Abstractions;

/// <summary>Talks to the Payment service for the checkout saga.</summary>
public interface IPaymentClient
{
    /// <summary>Charges the buyer for an order.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The order being paid.</param>
    /// <param name="amountMinor">Amount in minor currency units.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key for the charge.</param>
    /// <param name="paymentMethodId">The Stripe payment-method id tokenized client-side (PCI SAQ-A).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The payment result.</returns>
    Task<PaymentResult> ChargeAsync(
        Guid tenantId,
        Guid orderId,
        long amountMinor,
        string currency,
        string idempotencyKey,
        string paymentMethodId,
        CancellationToken cancellationToken);

    /// <summary>Refunds a charge (compensation).</summary>
    /// <param name="orderId">The order to refund.</param>
    /// <param name="idempotencyKey">Idempotency key for the refund.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the refund is requested.</returns>
    Task RefundAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken);
}
