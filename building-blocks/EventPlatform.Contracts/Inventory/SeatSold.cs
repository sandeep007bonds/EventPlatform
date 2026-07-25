namespace EventPlatform.Contracts.Inventory;

/// <summary>
/// Published by the Inventory service when a hold is converted to a sale. Consumed by Ticketing,
/// Reporting, etc.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the sale belongs to.</param>
/// <param name="HoldId">The hold that was converted.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="OrderId">The order the hold was converted for.</param>
/// <param name="SeatIds">The sold seat ids.</param>
public sealed record SeatSold(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid HoldId,
    Guid CatalogEventId,
    Guid OrderId,
    IReadOnlyList<Guid> SeatIds) : IntegrationEvent(EventId, OccurredAt, TenantId);
