namespace Payments.Application.Abstractions;

/// <summary>
/// A verified, provider-neutral payment webhook notification. The Infrastructure layer verifies the
/// provider signature and translates the raw event into this shape so the Application layer stays
/// free of any specific PSP SDK.
/// </summary>
/// <param name="EventId">
/// The provider's unique event id, used to deduplicate at-least-once delivery (exactly-once
/// processing).
/// </param>
/// <param name="EventType">The raw provider event type, recorded for diagnostics.</param>
/// <param name="Kind">What the event means in provider-neutral terms.</param>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id) to correlate.</param>
/// <param name="FailureReason">The failure reason when <see cref="Kind"/> is
/// <see cref="PaymentWebhookKind.Failed"/>; otherwise <see langword="null"/>.</param>
public sealed record PaymentWebhookNotification(
    string EventId,
    string EventType,
    PaymentWebhookKind Kind,
    string ProviderReference,
    string? FailureReason);
