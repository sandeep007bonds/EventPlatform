namespace EventPlatform.Contracts.Inventory;

/// <summary>
/// Published by the Inventory service when seats are held for a buyer. Consumed by Order,
/// Reporting, etc.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the hold belongs to.</param>
/// <param name="HoldId">The hold id.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="EventSessionId">
/// The performance the seats belong to — the grain inventory, orders and tickets are keyed by
/// (ADR-0039). <see cref="CatalogEventId"/> travels alongside it because the per-buyer ticket
/// limit is counted across the whole run.
/// </param>
/// <param name="UserId">The buyer holding the seats.</param>
/// <param name="ExpiresAt">When the hold expires (UTC).</param>
/// <param name="SeatIds">The held seat ids.</param>
public sealed record SeatHeld(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid HoldId,
    Guid CatalogEventId,
    Guid EventSessionId,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<Guid> SeatIds) : IntegrationEvent(EventId, OccurredAt, TenantId);
