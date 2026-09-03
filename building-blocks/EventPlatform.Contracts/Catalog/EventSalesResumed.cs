namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an organizer resumes sales for a paused performance.
/// Consumed by Inventory to allow new holds again.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the performance belongs to.</param>
/// <param name="CatalogEventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance whose sales have resumed.</param>
public sealed record EventSalesResumed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId) : IntegrationEvent(EventId, OccurredAt, TenantId);
