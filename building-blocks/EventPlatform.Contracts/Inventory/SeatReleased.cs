namespace EventPlatform.Contracts.Inventory;

/// <summary>
/// Published by the Inventory service when a hold is released and its seats return to available.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the hold belonged to.</param>
/// <param name="HoldId">The released hold id.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="SeatIds">The released seat ids.</param>
public sealed record SeatReleased(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid HoldId,
    Guid CatalogEventId,
    IReadOnlyList<Guid> SeatIds) : IntegrationEvent(EventId, OccurredAt, TenantId);
