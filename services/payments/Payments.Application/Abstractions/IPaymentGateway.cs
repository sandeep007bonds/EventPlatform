namespace Payments.Application.Abstractions;

/// <summary>
/// A payment service provider (PSP) gateway. The dev implementation simulates capture; a
/// Stripe-backed implementation drops in behind this port without touching the saga.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>The provider name recorded on the payment (e.g. <c>stripe-test</c>).</summary>
    string Provider { get; }

    /// <summary>Charges the given amount.</summary>
    /// <param name="amountMinor">Amount in minor currency units.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key passed to the PSP.</param>
    /// <param name="paymentMethodId">The payment-method id to charge (tokenized client-side, PCI SAQ-A).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The gateway result.</returns>
    Task<GatewayResult> ChargeAsync(
        long amountMinor,
        string currency,
        string idempotencyKey,
        string paymentMethodId,
        CancellationToken cancellationToken);

    /// <summary>Refunds a captured charge.</summary>
    /// <param name="providerReference">The provider reference to refund.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the refund is requested.</returns>
    Task RefundAsync(string providerReference, CancellationToken cancellationToken);
}
