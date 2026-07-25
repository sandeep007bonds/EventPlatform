namespace EventPlatform.Contracts.Payments;

/// <summary>Published by the Payment service when a charge fails.</summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the payment belongs to.</param>
/// <param name="PaymentId">The payment id.</param>
/// <param name="OrderId">The order that failed to pay.</param>
/// <param name="Reason">Why the charge failed.</param>
public sealed record PaymentFailed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid PaymentId,
    Guid OrderId,
    string Reason) : IntegrationEvent(EventId, OccurredAt, TenantId);
