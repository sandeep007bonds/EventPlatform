namespace Ordering.Workflow;

/// <summary>Output of the create-payment-intent activity.</summary>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
/// <param name="ClientSecret">The PSP client secret, used by the frontend to mount Payment Element.</param>
public sealed record CreateIntentOutput(string ProviderReference, string ClientSecret);
