namespace Catalog.Application.Features.DeactivateTicketType;

/// <summary>Retires a ticket type so it is no longer offered.</summary>
/// <param name="EventId">The event the type belongs to.</param>
/// <param name="TicketTypeId">The type to deactivate.</param>
/// <param name="TenantId">The calling tenant; must own the event.</param>
public sealed record DeactivateTicketTypeCommand(Guid EventId, Guid TicketTypeId, Guid TenantId)
    : IRequest<DeactivateTicketTypeOutcome>;
