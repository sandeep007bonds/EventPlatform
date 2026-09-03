namespace Catalog.Application.Features.ChangeEventSales;

/// <summary>Command to pause or resume sales across every one of an event's performances.</summary>
/// <remarks>
/// The event-wide switch, for when the whole run has to stop. Pulling a single night is
/// <c>ChangeSessionSales</c> instead.
/// </remarks>
/// <param name="EventId">The event to change.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="Pause"><see langword="true"/> to pause sales, <see langword="false"/> to resume.</param>
public sealed record ChangeEventSalesCommand(Guid EventId, Guid TenantId, bool Pause)
    : IRequest<ChangeEventSalesOutcome>;
