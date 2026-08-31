namespace Catalog.Api.Endpoints;

/// <summary>Request body for updating a ticket type.</summary>
/// <param name="Name">Buyer-facing name; must stay unique within the event.</param>
/// <param name="PriceMinor">
/// Price per ticket in minor units. Changing it is rejected with a 409 once the event is published —
/// Inventory holds its own copy of the price, so a change here would otherwise move the displayed
/// price without moving the charged one.
/// </param>
/// <param name="Description">Buyer-facing note, or null to clear it.</param>
/// <param name="SalesStartsAt">Earliest sellable instant, or null.</param>
/// <param name="SalesEndsAt">Latest sellable instant, or null.</param>
/// <param name="MaxPerBuyer">Per-buyer cap, or null for none.</param>
/// <param name="SortOrder">Display order; lower sorts first.</param>
public sealed record UpdateTicketTypeRequest(
    string Name,
    long PriceMinor,
    string? Description,
    DateTimeOffset? SalesStartsAt,
    DateTimeOffset? SalesEndsAt,
    int? MaxPerBuyer,
    int SortOrder);
