namespace Catalog.Application.Features.ResumeSales;

/// <summary>Command to resume sales for a published event previously paused.</summary>
/// <param name="Id">The event id to resume.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
public sealed record ResumeSalesCommand(Guid Id, Guid TenantId) : IRequest<ResumeSalesOutcome>;
