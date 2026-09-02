namespace Venues.Application;

/// <summary>Something a venue offers, as returned by the API.</summary>
/// <param name="Id">Facility id.</param>
/// <param name="Name">Facility name.</param>
/// <param name="Description">Optional detail shown alongside the name.</param>
public sealed record VenueFacilityResponse(Guid Id, string Name, string? Description);
