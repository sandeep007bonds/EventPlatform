namespace Catalog.Application.Features.UpdateTicketType;

/// <summary>Updates a ticket type's name, price, rules and presentation.</summary>
/// <param name="EventId">The event the type belongs to.</param>
/// <param name="TicketTypeId">The type to update.</param>
/// <param name="TenantId">The calling tenant; must own the event.</param>
/// <param name="Name">New buyer-facing name; must stay unique within the event.</param>
/// <param name="PriceMinor">New price in minor units. Only applied while the event is a draft.</param>
/// <param name="Description">Buyer-facing note, or null to clear it.</param>
/// <param name="SalesStartsAt">Earliest sellable instant, or null.</param>
/// <param name="SalesEndsAt">Latest sellable instant, or null.</param>
/// <param name="MaxPerBuyer">Per-buyer cap, or null for none.</param>
/// <param name="SortOrder">Display order; lower sorts first.</param>
public sealed record UpdateTicketTypeCommand(
    Guid EventId,
    Guid TicketTypeId,
    Guid TenantId,
    string Name,
    long PriceMinor,
    string? Description,
    DateTimeOffset? SalesStartsAt,
    DateTimeOffset? SalesEndsAt,
    int? MaxPerBuyer,
    int SortOrder) : IRequest<UpdateTicketTypeOutcome>;
