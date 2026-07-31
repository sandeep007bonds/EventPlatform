namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when a draft event's descriptive/promotional details
/// (description, venue, media, category, scheduling) are changed. Consumed by Search, CDN
/// cache invalidation, etc. once those exist — no consumer is wired up yet.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the catalog event belongs to.</param>
/// <param name="CatalogEventId">The id of the updated catalog event.</param>
public sealed record EventUpdated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId) : IntegrationEvent(EventId, OccurredAt, TenantId);
