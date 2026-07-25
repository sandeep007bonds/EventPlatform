namespace Ordering.Infrastructure;

/// <summary>
/// Talks to the Payment service (app-id <c>payments</c>) over Dapr service invocation, behind the
/// <see cref="IPaymentClient"/> port used by the checkout saga.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprPaymentClient(DaprClient daprClient) : IPaymentClient
{
    private const string PaymentsAppId = "payments";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<PaymentResult> ChargeAsync(
        Guid tenantId,
        Guid orderId,
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var http = daprClient.CreateInvokeHttpClient(PaymentsAppId);
        using var response = await http.PostAsJsonAsync(
            "v1/payments/charge",
            new { tenantId, orderId, amountMinor, currency, idempotencyKey },
            JsonOptions,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var failed = await response.Content.ReadFromJsonAsync<PaymentChargeResponse>(JsonOptions, cancellationToken);
            return new PaymentResult(Succeeded: false, PaymentReference: null, FailureReason: failed?.FailureReason ?? "payment_failed");
        }

        response.EnsureSuccessStatusCode();

        var captured = await response.Content.ReadFromJsonAsync<PaymentChargeResponse>(JsonOptions, cancellationToken);
        return new PaymentResult(Succeeded: true, PaymentReference: captured?.ProviderReference, FailureReason: null);
    }

    /// <inheritdoc />
    public async Task RefundAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var http = daprClient.CreateInvokeHttpClient(PaymentsAppId);
        // Compensation is best-effort; a failed refund is retried by ops, not the saga.
        using var response = await http.PostAsJsonAsync(
            "v1/payments/refund",
            new { orderId },
            JsonOptions,
            cancellationToken);
    }
}
