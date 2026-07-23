namespace EventPlatform.Contracts;

/// <summary>
/// Base type for all integration events published on the event bus. Every event
/// carries an id for idempotent consumption, a timestamp, and the owning tenant.
/// </summary>
/// <param name="EventId">Unique id of this event instance; used as the dedupe/idempotency key.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant the event belongs to.</param>
public abstract record IntegrationEvent(Guid EventId, DateTimeOffset OccurredAt, Guid TenantId);
