namespace Catalog.Application.Features.CreateTicketType;

/// <summary>Creates a named, priced ticket type for an event.</summary>
/// <param name="EventId">The event the type belongs to.</param>
/// <param name="TenantId">The calling tenant; must own the event.</param>
/// <param name="Name">Buyer-facing name, unique within the event (case-insensitively).</param>
/// <param name="PriceMinor">Price per ticket in minor currency units.</param>
/// <param name="Description">Buyer-facing note on what the ticket includes.</param>
/// <param name="SalesStartsAt">Earliest sellable instant, or null for the event's own bound.</param>
/// <param name="SalesEndsAt">Latest sellable instant, or null for the event's own bound.</param>
/// <param name="MaxPerBuyer">Per-buyer cap for this type, or null for none.</param>
/// <param name="SortOrder">Display order; lower sorts first.</param>
public sealed record CreateTicketTypeCommand(
    Guid EventId,
    Guid TenantId,
    string Name,
    long PriceMinor,
    string? Description = null,
    DateTimeOffset? SalesStartsAt = null,
    DateTimeOffset? SalesEndsAt = null,
    int? MaxPerBuyer = null,
    int SortOrder = 0) : IRequest<CreateTicketTypeResult>;
