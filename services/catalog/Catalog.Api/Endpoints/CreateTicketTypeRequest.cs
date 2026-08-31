namespace Catalog.Api.Endpoints;

/// <summary>Request body for creating a ticket type.</summary>
/// <param name="Name">Buyer-facing name, unique within the event (case-insensitively).</param>
/// <param name="PriceMinor">Price per ticket in minor currency units (e.g. 250000 for ₹2,500).</param>
/// <param name="Description">Buyer-facing note on what the ticket includes.</param>
/// <param name="SalesStartsAt">Earliest sellable instant; narrows the event's own on-sale window.</param>
/// <param name="SalesEndsAt">Latest sellable instant; narrows the event's own booking cutoff.</param>
/// <param name="MaxPerBuyer">Per-buyer cap for this type, on top of the event's overall limit.</param>
/// <param name="SortOrder">Display order; lower sorts first.</param>
public sealed record CreateTicketTypeRequest(
    string Name,
    long PriceMinor,
    string? Description = null,
    DateTimeOffset? SalesStartsAt = null,
    DateTimeOffset? SalesEndsAt = null,
    int? MaxPerBuyer = null,
    int SortOrder = 0);
