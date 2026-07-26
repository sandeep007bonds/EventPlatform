namespace EventPlatform.Contracts.Ticketing;

/// <summary>
/// Published by the Ticketing service when a ticket is issued for a sold seat. Consumed by
/// notifications, Reporting, etc.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the ticket belongs to.</param>
/// <param name="TicketId">The issued ticket id.</param>
/// <param name="OrderId">The order the ticket was issued for.</param>
/// <param name="CatalogEventId">The show/event the seat belongs to.</param>
/// <param name="SeatId">The seat the ticket admits.</param>
/// <param name="UserId">The ticket holder.</param>
public sealed record TicketIssued(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid TicketId,
    Guid OrderId,
    Guid CatalogEventId,
    Guid SeatId,
    Guid UserId) : IntegrationEvent(EventId, OccurredAt, TenantId);
