namespace EventPlatform.Contracts.Ordering;

/// <summary>
/// Published by the Order service when a checkout completes (paid and seats sold). Consumed by
/// Ticketing (to issue tickets), Reporting, etc.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the order belongs to.</param>
/// <param name="OrderId">The confirmed order id.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="UserId">The buyer.</param>
/// <param name="TotalMinor">Order total in minor currency units.</param>
/// <param name="Currency">Pricing currency (ISO 4217).</param>
/// <param name="Lines">The purchased lines — reserved seats and/or general-admission quantities.</param>
public sealed record OrderConfirmed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid OrderId,
    Guid CatalogEventId,
    Guid UserId,
    long TotalMinor,
    string Currency,
    IReadOnlyList<OrderLineSummary> Lines) : IntegrationEvent(EventId, OccurredAt, TenantId);
