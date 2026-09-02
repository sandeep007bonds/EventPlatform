namespace Venues.Api.Endpoints;

/// <summary>One unreserved capacity area in a submitted layout.</summary>
/// <param name="Code">Short stable code, unique within the version across sections and areas.</param>
/// <param name="Name">Display name.</param>
/// <param name="Capacity">How many people the area physically holds.</param>
/// <param name="DisplayOrder">Ordering when areas are listed.</param>
/// <param name="GateId">The gate this area is entered through, or <see langword="null"/> for any.</param>
public sealed record SeatMapAdmissionAreaRequest(
    string Code,
    string Name,
    int Capacity,
    int DisplayOrder,
    Guid? GateId);
