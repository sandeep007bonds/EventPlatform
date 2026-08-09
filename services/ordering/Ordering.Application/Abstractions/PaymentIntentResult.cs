namespace Ordering.Application.Abstractions;

/// <summary>Result of creating a payment intent for an order.</summary>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
/// <param name="ClientSecret">The PSP client secret, used by the frontend to mount Payment Element.</param>
public sealed record PaymentIntentResult(string ProviderReference, string ClientSecret);
