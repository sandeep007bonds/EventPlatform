namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an organizer manually pauses sales for a performance that
/// is on sale. Consumed by Inventory to reject new holds for it until sales resume, without
/// affecting already-placed holds or tickets.
/// </summary>
/// <remarks>
/// Per performance, not per event. Pulling one night of a run — a technical problem in the hall,
/// a cast change — should not stop the other nights selling. Pausing the whole event emits one of
/// these per performance.
/// </remarks>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the performance belongs to.</param>
/// <param name="CatalogEventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance whose sales are paused.</param>
public sealed record EventSalesPaused(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId) : IntegrationEvent(EventId, OccurredAt, TenantId);
