namespace EventPlatform.Contracts.Payments;

/// <summary>Published by the Payment service when a charge is captured.</summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the payment belongs to.</param>
/// <param name="PaymentId">The payment id.</param>
/// <param name="OrderId">The order that was paid.</param>
/// <param name="AmountMinor">Captured amount in minor currency units.</param>
/// <param name="Currency">Pricing currency (ISO 4217).</param>
/// <param name="ProviderReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
public sealed record PaymentCaptured(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid PaymentId,
    Guid OrderId,
    long AmountMinor,
    string Currency,
    string ProviderReference) : IntegrationEvent(EventId, OccurredAt, TenantId);
