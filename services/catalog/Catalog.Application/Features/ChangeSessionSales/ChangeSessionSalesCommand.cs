namespace Catalog.Application.Features.ChangeSessionSales;

/// <summary>Command to pause or resume sales for one performance.</summary>
/// <remarks>
/// One command with a target rather than two, because the two are the same decision seen from
/// opposite ends and share every guard. The API still exposes them as two verbs, since that is
/// what the caller means.
/// </remarks>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to change.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Pause"><see langword="true"/> to pause sales, <see langword="false"/> to resume.</param>
public sealed record ChangeSessionSalesCommand(Guid EventId, Guid EventSessionId, Guid TenantId, bool Pause)
    : IRequest<SessionCommandResult>;
