namespace EventPlatform.Contracts.Payments;

/// <summary>Published by the Payment service when a captured charge is refunded.</summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the payment belongs to.</param>
/// <param name="PaymentId">The payment id.</param>
/// <param name="OrderId">The order that was refunded.</param>
/// <param name="AmountMinor">Refunded amount in minor currency units.</param>
public sealed record PaymentRefunded(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid PaymentId,
    Guid OrderId,
    long AmountMinor) : IntegrationEvent(EventId, OccurredAt, TenantId);
