namespace Payments.Application.Abstractions;

/// <summary>
/// Verifies and parses an inbound payment-provider webhook. The implementation checks the provider
/// signature against the configured webhook secret and translates the payload into a neutral
/// <see cref="PaymentWebhookNotification"/>.
/// </summary>
public interface IPaymentWebhookGateway
{
    /// <summary>
    /// Verifies the webhook signature and parses the payload.
    /// </summary>
    /// <param name="payload">The raw request body exactly as received (unbuffered).</param>
    /// <param name="signatureHeader">The provider signature header value.</param>
    /// <returns>
    /// The parsed notification, or <see langword="null"/> when the event type is not one we act on.
    /// </returns>
    /// <exception cref="PaymentWebhookVerificationException">The signature is missing or invalid.</exception>
    PaymentWebhookNotification? Verify(string payload, string? signatureHeader);
}
