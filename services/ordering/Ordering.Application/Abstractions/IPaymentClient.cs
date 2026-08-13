namespace Ordering.Application.Abstractions;

/// <summary>Talks to the Payment service for the checkout saga.</summary>
public interface IPaymentClient
{
    /// <summary>
    /// Creates a payment intent for an order, without confirming it — the buyer confirms
    /// client-side via Stripe's Payment Element; the outcome arrives later via webhook.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The order being paid.</param>
    /// <param name="amountMinor">Amount in minor currency units.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key for the intent.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created intent's reference and client secret.</returns>
    Task<PaymentIntentResult> CreateIntentAsync(
        Guid tenantId,
        Guid orderId,
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asks Payments to re-read the order's payment from the provider and reconcile it. The pull
    /// counterpart to the provider's webhook — lets the saga learn an outcome without depending on
    /// the provider being able to call back into this environment.
    /// </summary>
    /// <param name="orderId">The order whose payment to re-read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>NotFound</c>, <c>Pending</c>, <c>Captured</c> or <c>Failed</c>.</returns>
    Task<string> SyncStatusAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Refunds a charge (compensation).</summary>
    /// <param name="orderId">The order to refund.</param>
    /// <param name="idempotencyKey">Idempotency key for the refund.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the refund is requested.</returns>
    Task RefundAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken);
}
