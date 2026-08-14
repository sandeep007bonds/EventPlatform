namespace Payments.Infrastructure;

/// <summary>
/// Stripe-backed <see cref="IPaymentGateway"/> (test or live, per the configured secret key).
/// Card data never touches our servers (PCI SAQ-A): we only ever create a PaymentIntent server side
/// (never confirm it) — the client attaches and authenticates a payment method (card, UPI, etc.)
/// entirely client-side via Stripe's Payment Element, against the returned client secret. The
/// resulting capture/decline is reported later by Stripe's webhook, not by this call. The secret key
/// comes from configuration (Key Vault in cloud, user-secrets/env locally) — never from code.
/// </summary>
internal sealed class StripePaymentGateway : IPaymentGateway
{
    // Stripe.net's own HttpClient carries no timeout by default, so a network-level hang (blocked
    // outbound HTTPS, DNS failure, an unreachable proxy, etc.) would otherwise sit for .NET's
    // HttpClient default of 100 seconds — long enough to blow past the gateway's own forwarding
    // timeout and surface as a confusing 499/504 instead of a fast, clear failure.
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

    private readonly PaymentIntentService paymentIntents;
    private readonly RefundService refunds;

    /// <summary>Creates the gateway for the given Stripe secret key.</summary>
    /// <param name="secretKey">The Stripe secret key (from configuration).</param>
    public StripePaymentGateway(string secretKey)
    {
        var httpClient = new HttpClient { Timeout = RequestTimeout };
        var client = new StripeClient(secretKey, httpClient: new SystemNetHttpClient(httpClient));
        paymentIntents = new PaymentIntentService(client);
        refunds = new RefundService(client);
    }

    /// <inheritdoc />
    public string Provider => "stripe";

    /// <inheritdoc />
    public async Task<GatewayIntentResult> CreateIntentAsync(
        long amountMinor,
        string currency,
        string idempotencyKey,
        string description,
        CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = ToStripeCurrency(currency),

            // Required for export transactions from an India-registered Stripe account (RBI rules) —
            // without it Stripe rejects the payment outright, not just at the dashboard level.
            Description = description,

            // Deliberately no PaymentMethod/PaymentMethodTypes/Confirm — Payment Element attaches and
            // confirms client-side. AutomaticPaymentMethods surfaces every method enabled for the
            // account's region/currency (cards, UPI, and anything else configured in the Stripe
            // Dashboard), not just cards. Note UPI is INR-only: a non-INR charge will only ever
            // surface cards, however the account is configured.
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
        };

        // Stripe idempotency: a retried create with the same key returns the same PaymentIntent.
        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };

        var intent = await paymentIntents.CreateAsync(options, requestOptions, cancellationToken);

        return new GatewayIntentResult(intent.Id, intent.ClientSecret, CapturedImmediately: false);
    }

    /// <inheritdoc />
    public async Task<GatewayPaymentStatus> GetStatusAsync(string providerReference, CancellationToken cancellationToken)
    {
        var intent = await paymentIntents.GetAsync(providerReference, cancellationToken: cancellationToken);

        // Stripe's PaymentIntent statuses: requires_payment_method, requires_confirmation,
        // requires_action, processing, requires_capture, succeeded, canceled.
        return intent.Status switch
        {
            "succeeded" => GatewayPaymentStatus.Captured,
            "canceled" => GatewayPaymentStatus.Failed,
            _ => GatewayPaymentStatus.Pending,
        };
    }

    /// <inheritdoc />
    public async Task<bool> TryCancelAsync(string providerReference, CancellationToken cancellationToken)
    {
        try
        {
            await paymentIntents.CancelAsync(providerReference, cancellationToken: cancellationToken);
            return true;
        }
        catch (StripeException)
        {
            // Stripe refuses to cancel an intent that is no longer cancellable — overwhelmingly
            // because it succeeded between our last read and this call. Swallowed deliberately:
            // this is a best-effort tidy-up, and reporting failure lets the caller re-read rather
            // than mark a payment failed that may be holding the buyer's money.
            return false;
        }
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
