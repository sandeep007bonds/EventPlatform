namespace Ticketing.Api.Endpoints;

/// <summary>Read model returned for a ticket.</summary>
/// <param name="Id">Ticket id.</param>
/// <param name="OrderId">The order the ticket was issued for.</param>
/// <param name="CatalogEventId">The show/event the seat belongs to.</param>
/// <param name="SeatId">The seat the ticket admits.</param>
/// <param name="Token">Opaque scan token encoded in the QR code.</param>
/// <param name="Status">Ticket status name.</param>
/// <param name="IssuedAt">When the ticket was issued (UTC).</param>
public sealed record TicketResponse(
    Guid Id,
    Guid OrderId,
    Guid CatalogEventId,
    Guid SeatId,
    string Token,
    string Status,
    DateTimeOffset IssuedAt);
