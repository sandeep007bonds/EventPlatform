namespace Venues.Api.Endpoints;

/// <summary>Request body for adding a physical entry point to a venue.</summary>
/// <param name="Code">Short stable code, unique within the venue (e.g. <c>G3</c>).</param>
/// <param name="Name">Display name (e.g. <c>Gate 3 — North</c>).</param>
public sealed record AddVenueGateRequest(string Code, string Name);
