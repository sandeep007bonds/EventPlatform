namespace Inventory.Api.Endpoints;

/// <summary>
/// One seat's current availability, plus what it costs and which ticket type it sells as.
/// </summary>
/// <remarks>
/// The price is here because it is the price Inventory actually provisioned, resolved once at
/// publish time by joining the Venue seat map to the performance's allocation map. A buyer's seat
/// picker showing anything else — a price the SPA re-derived from Catalog, say — could quote a
/// number the checkout then refuses (ADR-0034: prices come from the server, always).
/// </remarks>
/// <param name="SeatId">The Venue seat.</param>
/// <param name="Status">Availability status name (<c>Available</c>, <c>Held</c>, <c>Sold</c>, <c>Blocked</c>).</param>
/// <param name="TicketTypeId">The Catalog ticket type this seat sells as for this performance.</param>
/// <param name="PriceMinor">The seat's price in minor units.</param>
public sealed record InventorySeatResponse(
    Guid SeatId,
    string Status,
    Guid TicketTypeId,
    long PriceMinor);
