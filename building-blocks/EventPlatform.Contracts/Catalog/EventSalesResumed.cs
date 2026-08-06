namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an organizer resumes sales for a published event
/// previously paused via <c>EventSalesPaused</c>. Consumed by Inventory to allow new holds for the
/// event again.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the catalog event belongs to.</param>
/// <param name="CatalogEventId">The id of the resumed catalog event.</param>
public sealed record EventSalesResumed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId) : IntegrationEvent(EventId, OccurredAt, TenantId);
