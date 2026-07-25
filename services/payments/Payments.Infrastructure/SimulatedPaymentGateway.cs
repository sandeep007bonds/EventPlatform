namespace Payments.Infrastructure;

/// <summary>
/// Dev/test payment gateway that captures synchronously and always succeeds — the local stand-in
/// for Stripe test mode. The real Stripe-backed gateway (Stripe.net, secret key from Key Vault)
/// replaces this behind <see cref="IPaymentGateway"/> without touching the saga. See tracker T-stripe.
/// </summary>
internal sealed class SimulatedPaymentGateway : IPaymentGateway
{
    /// <inheritdoc />
    public string Provider => "stripe-test";

    /// <inheritdoc />
    public Task<GatewayResult> ChargeAsync(
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(new GatewayResult(Succeeded: true, Reference: $"pi_sim_{Guid.CreateVersion7():N}", FailureReason: null));

    /// <inheritdoc />
    public Task RefundAsync(string providerReference, CancellationToken cancellationToken) => Task.CompletedTask;
}
