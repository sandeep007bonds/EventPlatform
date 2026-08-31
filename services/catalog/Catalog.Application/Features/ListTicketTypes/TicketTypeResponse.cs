namespace Catalog.Application.Features.ListTicketTypes;

/// <summary>Read model for one ticket type.</summary>
/// <param name="Id">Stable ticket-type id, referenced by seat-map sections.</param>
/// <param name="Name">Buyer-facing name.</param>
/// <param name="PriceMinor">Price per ticket in minor currency units.</param>
/// <param name="Description">Buyer-facing note on what the ticket includes.</param>
/// <param name="SalesStartsAt">Earliest sellable instant, if this type narrows the event's window.</param>
/// <param name="SalesEndsAt">Latest sellable instant, if this type narrows the event's window.</param>
/// <param name="MaxPerBuyer">Per-buyer cap for this type, if any.</param>
/// <param name="SortOrder">Display order; lower sorts first.</param>
/// <param name="IsActive">Whether the type is still offered.</param>
public sealed record TicketTypeResponse(
    Guid Id,
    string Name,
    long PriceMinor,
    string? Description,
    DateTimeOffset? SalesStartsAt,
    DateTimeOffset? SalesEndsAt,
    int? MaxPerBuyer,
    int SortOrder,
    bool IsActive);
