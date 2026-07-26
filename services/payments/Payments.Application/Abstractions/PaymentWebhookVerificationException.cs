namespace Payments.Application.Abstractions;

/// <summary>
/// Thrown when an inbound payment webhook cannot be verified — a missing or invalid provider
/// signature. The endpoint maps this to <c>400 Bad Request</c> so the provider retries or alerts.
/// </summary>
public sealed class PaymentWebhookVerificationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="PaymentWebhookVerificationException"/> class.</summary>
    public PaymentWebhookVerificationException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PaymentWebhookVerificationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    public PaymentWebhookVerificationException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PaymentWebhookVerificationException"/> class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public PaymentWebhookVerificationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
