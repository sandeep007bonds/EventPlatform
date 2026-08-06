namespace Catalog.Application.Features.PauseSales;

/// <summary>Command to pause sales for a published event.</summary>
/// <param name="Id">The event id to pause.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
public sealed record PauseSalesCommand(Guid Id, Guid TenantId) : IRequest<PauseSalesOutcome>;
