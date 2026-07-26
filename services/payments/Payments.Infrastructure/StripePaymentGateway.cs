namespace Payments.Infrastructure;

/// <summary>
/// Stripe-backed <see cref="IPaymentGateway"/> (test or live, per the configured secret key).
/// Card data never touches our servers (PCI SAQ-A): we create and confirm a PaymentIntent server
/// side against a Stripe payment method. The secret key comes from configuration (Key Vault in
/// cloud, user-secrets/env locally) — never from code.
/// </summary>
internal sealed class StripePaymentGateway : IPaymentGateway
{
    // Stripe's test card payment method. In production the client collects and confirms the card
    // (SAQ-A); the payment-method id would arrive on the charge request instead. See tracker T-stripe.
    private const string TestPaymentMethod = "pm_card_visa";

    private readonly PaymentIntentService paymentIntents;
    private readonly RefundService refunds;

    /// <summary>Creates the gateway for the given Stripe secret key.</summary>
    /// <param name="secretKey">The Stripe secret key (from configuration).</param>
    public StripePaymentGateway(string secretKey)
    {
        var client = new StripeClient(secretKey);
        paymentIntents = new PaymentIntentService(client);
        refunds = new RefundService(client);
    }

    /// <inheritdoc />
    public string Provider => "stripe";

    /// <inheritdoc />
    public async Task<GatewayResult> ChargeAsync(
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = ToStripeCurrency(currency),
            PaymentMethod = TestPaymentMethod,
            PaymentMethodTypes = ["card"],
            Confirm = true,
        };

        // Stripe idempotency: a retried charge with the same key returns the same PaymentIntent.
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };

        var intent = await paymentIntents.CreateAsync(options, requestOptions, cancellationToken);

        // Always surface the PaymentIntent id (even when not yet captured) so the payment can record
        // it and a later `payment_intent.*` webhook correlates back — see StripeWebhookGateway.
        return string.Equals(intent.Status, "succeeded", StringComparison.Ordinal)
            ? new GatewayResult(Succeeded: true, Reference: intent.Id, FailureReason: null)
            : new GatewayResult(Succeeded: false, Reference: intent.Id, FailureReason: intent.Status);
    }

    /// <inheritdoc />
    public async Task RefundAsync(string providerReference, CancellationToken cancellationToken)
    {
        var options = new RefundCreateOptions { PaymentIntent = providerReference };
        await refunds.CreateAsync(options, cancellationToken: cancellationToken);
    }

    // Stripe requires lowercase ISO 4217 currency codes.
    private static string ToStripeCurrency(string currency)
    {
#pragma warning disable CA1308 // Stripe's API mandates lowercase currency codes.
        return currency.ToLowerInvariant();
#pragma warning restore CA1308
    }
}
