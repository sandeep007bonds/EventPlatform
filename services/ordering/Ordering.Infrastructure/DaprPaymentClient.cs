namespace Ordering.Infrastructure;

/// <summary>
/// Talks to the Payment service (app-id <c>payments</c>) over Dapr service invocation, behind the
/// <see cref="IPaymentClient"/> port used by the checkout saga.
/// </summary>
internal sealed class DaprPaymentClient : IPaymentClient
{
    private const string PaymentsAppId = "payments";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<PaymentIntentResult> CreateIntentAsync(
        Guid tenantId,
        Guid orderId,
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(PaymentsAppId);
        using var response = await http.PostAsJsonAsync(
            "v1/payments/intents",
            new { tenantId, orderId, amountMinor, currency, idempotencyKey },
            JsonOptions,
            cancellationToken);

        // A genuine hard failure here (bad amount, PSP outage) surfaces as an unhandled exception —
        // there is no structured failure response any more, the same implicit behavior this call
        // already had for an uncaught Stripe exception.
        response.EnsureSuccessStatusCode();

        var intent = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>(JsonOptions, cancellationToken);
        return new PaymentIntentResult(
            intent?.ProviderReference ?? throw new InvalidOperationException("Payments returned an empty create-intent response."),
            intent.ClientSecret);
    }

    /// <inheritdoc />
    public async Task RefundAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken)
    {
        // Compensation is best-effort; a failed refund is retried by ops, not the saga.
        using var http = DaprClient.CreateInvokeHttpClient(PaymentsAppId);
        using var response = await http.PostAsJsonAsync(
            "v1/payments/refund",
            new { orderId },
            JsonOptions,
            cancellationToken);
    }
}
