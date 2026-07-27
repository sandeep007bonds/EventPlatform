namespace EventPlatform.Contracts.Inventory;

/// <summary>Published by the Inventory service when a previously blocked seat is unblocked.</summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the seats belong to.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="SeatIds">The unblocked seat ids.</param>
public sealed record SeatUnblocked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    IReadOnlyList<Guid> SeatIds) : IntegrationEvent(EventId, OccurredAt, TenantId);
