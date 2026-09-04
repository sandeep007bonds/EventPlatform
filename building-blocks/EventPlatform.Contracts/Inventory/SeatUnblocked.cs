namespace EventPlatform.Contracts.Inventory;

/// <summary>Published by the Inventory service when a previously blocked seat is unblocked.</summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the seats belong to.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="EventSessionId">
/// The performance the seats belong to — the grain inventory, orders and tickets are keyed by
/// (ADR-0039). <see cref="CatalogEventId"/> travels alongside it because the per-buyer ticket
/// limit is counted across the whole run.
/// </param>
/// <param name="SeatIds">The unblocked seat ids.</param>
public sealed record SeatUnblocked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId,
    IReadOnlyList<Guid> SeatIds) : IntegrationEvent(EventId, OccurredAt, TenantId);
