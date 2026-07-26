namespace Payments.Infrastructure;

/// <summary>
/// Stripe-backed <see cref="IPaymentWebhookGateway"/>. Verifies the <c>Stripe-Signature</c> header
/// against the endpoint's webhook signing secret (never trusting the payload otherwise) and maps
/// the events we act on to the neutral <see cref="PaymentWebhookNotification"/>. The signing secret
/// comes from configuration (Key Vault in cloud, user-secrets/env locally) — never from code.
/// </summary>
internal sealed class StripeWebhookGateway : IPaymentWebhookGateway
{
    // Stripe event types we act on (raw strings, so we don't depend on an SDK constants class).
    private const string PaymentIntentSucceeded = "payment_intent.succeeded";
    private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    private const string ChargeRefunded = "charge.refunded";

    private readonly string webhookSecret;

    /// <summary>Creates the gateway for the given Stripe webhook signing secret.</summary>
    /// <param name="webhookSecret">The Stripe webhook signing secret (<c>whsec_…</c>).</param>
    public StripeWebhookGateway(string webhookSecret) => this.webhookSecret = webhookSecret;

    /// <inheritdoc />
    public PaymentWebhookNotification? Verify(string payload, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            throw new PaymentWebhookVerificationException("Missing Stripe-Signature header.");
        }

        Event stripeEvent;
        try
        {
            // Throws when the signature does not match the secret or the timestamp is outside
            // tolerance — i.e. the payload is not a genuine, timely Stripe delivery.
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, webhookSecret);
        }
        catch (StripeException ex)
        {
            throw new PaymentWebhookVerificationException("Invalid Stripe webhook signature.", ex);
        }

        if (stripeEvent.Type == PaymentIntentSucceeded && stripeEvent.Data.Object is PaymentIntent captured)
        {
            return new PaymentWebhookNotification(
                stripeEvent.Id,
                stripeEvent.Type,
                PaymentWebhookKind.Captured,
                captured.Id,
                FailureReason: null);
        }

        if (stripeEvent.Type == PaymentIntentPaymentFailed && stripeEvent.Data.Object is PaymentIntent failed)
        {
            return new PaymentWebhookNotification(
                stripeEvent.Id,
                stripeEvent.Type,
                PaymentWebhookKind.Failed,
                failed.Id,
                failed.LastPaymentError?.Message ?? "payment_failed");
        }

        if (stripeEvent.Type == ChargeRefunded &&
            stripeEvent.Data.Object is Charge charge &&
            charge.PaymentIntentId is not null)
        {
            return new PaymentWebhookNotification(
                stripeEvent.Id,
                stripeEvent.Type,
                PaymentWebhookKind.Refunded,
                charge.PaymentIntentId,
                FailureReason: null);
        }

        // An event type we do not act on — acknowledged and ignored.
        return null;
    }
}
