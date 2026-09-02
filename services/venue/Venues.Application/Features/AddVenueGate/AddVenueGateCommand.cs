namespace Venues.Application.Features.AddVenueGate;

/// <summary>Command to add a physical entry point to a venue.</summary>
/// <param name="VenueId">The venue to add the gate to.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Code">Short stable code, unique within the venue.</param>
/// <param name="Name">Display name.</param>
public sealed record AddVenueGateCommand(Guid VenueId, Guid TenantId, string Code, string Name)
    : IRequest<AddVenueGateResult>;
