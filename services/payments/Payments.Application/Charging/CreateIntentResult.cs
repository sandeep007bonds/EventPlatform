namespace Payments.Application.Charging;

/// <summary>Result of creating (or re-fetching an idempotent) payment intent for an order.</summary>
/// <param name="PaymentId">The payment id.</param>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
/// <param name="ClientSecret">The PSP client secret, used by the frontend to mount Payment Element.</param>
/// <param name="Captured">
/// Whether the payment was already captured synchronously (the simulated gateway's dev path) —
/// when <see langword="true"/>, the caller has nothing further to wait for.
/// </param>
public sealed record CreateIntentResult(Guid PaymentId, string ProviderReference, string ClientSecret, bool Captured);
