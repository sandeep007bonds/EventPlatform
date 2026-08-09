namespace Ordering.Infrastructure;

/// <summary>The Payments create-intent response deserialized by the payment client.</summary>
/// <param name="PaymentId">The payment id.</param>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
/// <param name="ClientSecret">The PSP client secret.</param>
/// <param name="Captured">Whether the payment was already captured synchronously (dev path).</param>
internal sealed record PaymentIntentResponse(Guid PaymentId, string ProviderReference, string ClientSecret, bool Captured);
