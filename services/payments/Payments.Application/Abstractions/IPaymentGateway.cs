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

    /// <summary>
    /// Reads the provider's current status for a payment. This is the pull counterpart to the
    /// provider's webhook: it lets us learn a payment's outcome by asking, rather than waiting to
    /// be told — which is what makes the flow work on a machine the provider can't call back
    /// (localhost), and a backstop if a webhook is ever missed in production.
    /// </summary>
    /// <param name="providerReference">The PSP reference to read (e.g. Stripe PaymentIntent id).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The provider's current status for that payment.</returns>
    Task<GatewayPaymentStatus> GetStatusAsync(string providerReference, CancellationToken cancellationToken);

    /// <summary>
    /// Best-effort cancellation of a payment that was never completed, releasing any authorization
    /// the buyer's bank is still holding. Returns <see langword="false"/> when the provider refuses
    /// — which almost always means the payment is no longer cancellable because it actually
    /// succeeded, so the caller must re-read rather than treat it as failed.
    /// </summary>
    /// <param name="providerReference">The PSP reference to cancel.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the provider accepted the cancellation.</returns>
    Task<bool> TryCancelAsync(string providerReference, CancellationToken cancellationToken);

    /// <summary>Refunds a captured charge.</summary>
    /// <param name="providerReference">The provider reference to refund.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the refund is requested.</returns>
    Task RefundAsync(string providerReference, long amountMinor, CancellationToken cancellationToken);
}
