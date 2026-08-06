namespace Catalog.Application.Features.CreateEntryGate;

/// <summary>
/// Command to define a new entry gate for an event. <see cref="TenantId"/> is set server-side
/// from the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="EventId">The event the gate belongs to.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="Name">Gate name.</param>
public sealed record CreateEntryGateCommand(Guid EventId, Guid TenantId, string Name) : IRequest<CreateEntryGateResult>;
