namespace Venues.Application;

/// <summary>An unreserved capacity area as returned by the API.</summary>
/// <param name="Id">Area id.</param>
/// <param name="Code">Short stable code, unique within the version.</param>
/// <param name="Name">Display name.</param>
/// <param name="Capacity">How many people the area physically holds.</param>
/// <param name="DisplayOrder">Ordering when areas are listed.</param>
/// <param name="GateId">The gate this area is entered through, if any.</param>
public sealed record AdmissionAreaResponse(
    Guid Id,
    string Code,
    string Name,
    int Capacity,
    int DisplayOrder,
    Guid? GateId);
