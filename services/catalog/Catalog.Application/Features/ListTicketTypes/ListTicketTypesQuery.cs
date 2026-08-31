namespace Catalog.Application.Features.ListTicketTypes;

/// <summary>Lists an event's ticket types, active or not — the organizer's view.</summary>
/// <param name="EventId">The event id.</param>
/// <param name="TenantId">The calling tenant; must own the event.</param>
public sealed record ListTicketTypesQuery(Guid EventId, Guid TenantId)
    : IRequest<IReadOnlyList<TicketTypeResponse>?>;
