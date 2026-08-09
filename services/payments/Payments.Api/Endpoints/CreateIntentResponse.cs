namespace Payments.Api.Endpoints;

/// <summary>Response for a payment-intent create call.</summary>
/// <param name="PaymentId">The payment id.</param>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
/// <param name="ClientSecret">The PSP client secret, used by the frontend to mount Payment Element.</param>
/// <param name="Captured">Whether the payment was already captured synchronously (dev path).</param>
public sealed record CreateIntentResponse(
    Guid PaymentId,
    string ProviderReference,
    string ClientSecret,
    bool Captured);
