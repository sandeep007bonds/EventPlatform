namespace Payments.Infrastructure;

/// <summary>
/// Dev/test payment gateway that captures synchronously and always succeeds — the local stand-in
/// for Stripe test mode. The real Stripe-backed gateway (Stripe.net, secret key from Key Vault)
/// replaces this behind <see cref="IPaymentGateway"/> without touching the saga.
/// </summary>
internal sealed class SimulatedPaymentGateway : IPaymentGateway
{
    /// <inheritdoc />
    public string Provider => "stripe-test";

    /// <inheritdoc />
    public Task<GatewayIntentResult> CreateIntentAsync(
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var reference = $"pi_sim_{Guid.CreateVersion7():N}";
        var clientSecret = $"{reference}_secret_sim";

        // Self-confirms synchronously, exactly as before — but the caller still drives the resulting
        // capture through the same outbox -> pub/sub -> Ordering-subscriber path real Stripe uses, so
        // there is no fork in Ordering's saga logic between dev and prod.
        return Task.FromResult(new GatewayIntentResult(reference, clientSecret, CapturedImmediately: true));
    }

    /// <inheritdoc />
    public Task RefundAsync(string providerReference, CancellationToken cancellationToken) => Task.CompletedTask;
}
