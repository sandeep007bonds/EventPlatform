namespace Payments.Application.Abstractions;

/// <summary>
/// Result of creating a payment-gateway intent. Unlike the old synchronous charge result, this
/// carries no success/failure branch — a real intent-creation failure (bad amount, PSP outage) is a
/// genuine exception, left to propagate; the eventual capture/decline outcome, once the buyer
/// authenticates, arrives later via the provider's webhook.
/// </summary>
/// <param name="Reference">The provider reference (e.g. Stripe PaymentIntent id).</param>
/// <param name="ClientSecret">The client secret the frontend uses to mount Stripe's Payment Element.</param>
/// <param name="CapturedImmediately">
/// Whether the gateway captured the payment synchronously at creation time (the simulated gateway
/// always does; a real Stripe intent never does, since it is created without <c>Confirm</c>).
/// </param>
public sealed record GatewayIntentResult(string Reference, string ClientSecret, bool CapturedImmediately);
