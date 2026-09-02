namespace Venues.Application;

/// <summary>A venue's physical entry point as returned by the API.</summary>
/// <param name="Id">Gate id.</param>
/// <param name="Code">Short stable code, unique within the venue.</param>
/// <param name="Name">Display name.</param>
/// <param name="IsActive">Whether the gate is currently in use.</param>
public sealed record VenueGateResponse(Guid Id, string Code, string Name, bool IsActive);
