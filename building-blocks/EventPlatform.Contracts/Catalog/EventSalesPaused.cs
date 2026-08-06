namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an organizer manually pauses sales for a published event.
/// Consumed by Inventory to reject new holds for the event until sales resume, without affecting
/// already-placed holds/tickets.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the catalog event belongs to.</param>
/// <param name="CatalogEventId">The id of the paused catalog event.</param>
public sealed record EventSalesPaused(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId) : IntegrationEvent(EventId, OccurredAt, TenantId);
