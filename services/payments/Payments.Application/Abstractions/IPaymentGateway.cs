namespace Payments.Application.Abstractions;

/// <summary>
/// A payment service provider (PSP) gateway. The dev implementation simulates capture; a
/// Stripe-backed implementation drops in behind this port without touching the saga.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>The provider name recorded on the payment (e.g. <c>stripe-test</c>).</summary>
    string Provider { get; }

    /// <summary>
    /// Creates a payment intent for the given amount, without confirming it. Confirmation (attaching
    /// and authenticating a payment method — card, UPI, etc.) happens client-side against the
    /// returned client secret via Stripe's Payment Element; the outcome is reported later by the
    /// provider's webhook, not returned here.
    /// </summary>
    /// <param name="amountMinor">Amount in minor currency units.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key passed to the PSP.</param>
    /// <param name="description">
    /// What the buyer is paying for. Surfaced on the PSP dashboard, and **required** by Stripe for
    /// export transactions from an India-registered account (RBI rules) — a missing description
    /// fails those charges outright, so this is not merely cosmetic.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The created intent's reference and client secret.</returns>
    Task<GatewayIntentResult> CreateIntentAsync(
        long amountMinor,
        string currency,
        string idempotencyKey,
        string description,
        CancellationToken cancellationToken);

    /// <summary>Refunds a captured charge.</summary>
    /// <param name="providerReference">The provider reference to refund.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the refund is requested.</returns>
    Task RefundAsync(string providerReference, CancellationToken cancellationToken);
}
