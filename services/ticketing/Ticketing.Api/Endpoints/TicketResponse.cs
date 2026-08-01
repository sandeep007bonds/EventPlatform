namespace Ticketing.Api.Endpoints;

/// <summary>Read model returned for a ticket.</summary>
/// <param name="Id">Ticket id.</param>
/// <param name="OrderId">The order the ticket was issued for.</param>
/// <param name="CatalogEventId">The show/event the seat belongs to.</param>
/// <param name="SeatId">The seat the ticket admits, if this ticket is for a reserved seat.</param>
/// <param name="GeneralAdmissionAllocationId">The allocation the ticket admits, if this ticket is general admission.</param>
/// <param name="Token">Opaque scan token encoded in the QR code.</param>
/// <param name="Status">Ticket status name.</param>
/// <param name="IssuedAt">When the ticket was issued (UTC).</param>
/// <param name="CheckedInAt">When the ticket was scanned/checked in at the gate (UTC), if it has been.</param>
public sealed record TicketResponse(
    Guid Id,
    Guid OrderId,
    Guid CatalogEventId,
    Guid? SeatId,
    Guid? GeneralAdmissionAllocationId,
    string Token,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? CheckedInAt);
