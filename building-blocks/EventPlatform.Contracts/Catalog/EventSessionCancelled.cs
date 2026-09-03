namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when a performance that was on sale is called off.
/// </summary>
/// <remarks>
/// Says only that it happened. Working out who bought what and giving their money back is a saga
/// with approval and compensation in it, not a side effect of a status change — consumers stop
/// selling against the performance; refunds are driven separately.
/// </remarks>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the performance belongs to.</param>
/// <param name="CatalogEventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The cancelled performance.</param>
public sealed record EventSessionCancelled(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId) : IntegrationEvent(EventId, OccurredAt, TenantId);
