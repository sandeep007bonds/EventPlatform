namespace Venues.Api.Endpoints;

/// <summary>Request body for recording something a venue offers.</summary>
/// <param name="Name">Facility name (e.g. <c>Step-free access</c>).</param>
/// <param name="Description">Optional detail shown alongside the name.</param>
public sealed record AddVenueFacilityRequest(string Name, string? Description);
