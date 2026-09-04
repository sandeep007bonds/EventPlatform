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
/// <param name="EventSessionId">
/// The performance the seats belong to — the grain inventory, orders and tickets are keyed by
/// (ADR-0039). <see cref="CatalogEventId"/> travels alongside it because the per-buyer ticket
/// limit is counted across the whole run.
/// </param>
/// <param name="SeatId">The seat the ticket admits, if this ticket is for a reserved seat.</param>
/// <param name="GeneralAdmissionAllocationId">The allocation the ticket admits, if this ticket is general admission.</param>
/// <param name="UserId">The ticket holder.</param>
public sealed record TicketIssued(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid TicketId,
    Guid OrderId,
    Guid CatalogEventId,
    Guid EventSessionId,
    Guid? SeatId,
    Guid? GeneralAdmissionAllocationId,
    Guid UserId) : IntegrationEvent(EventId, OccurredAt, TenantId);
