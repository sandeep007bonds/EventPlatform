namespace Ordering.Infrastructure;

/// <summary>
/// Dev stub for <see cref="IPaymentClient"/> — always succeeds. Replaced by the Stripe-backed
/// Payment service (issue #9).
/// </summary>
internal sealed class StubPaymentClient : IPaymentClient
{
    /// <inheritdoc />
    public Task<PaymentResult> ChargeAsync(
        Guid orderId,
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentResult(Succeeded: true, PaymentReference: $"stub_{orderId:N}", FailureReason: null));

    /// <inheritdoc />
    public Task RefundAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
