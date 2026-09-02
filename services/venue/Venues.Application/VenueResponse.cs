namespace Venues.Application;

/// <summary>A venue in full, as returned by the API.</summary>
/// <param name="Id">Venue id.</param>
/// <param name="TenantId">Owning tenant (organizer).</param>
/// <param name="Name">Venue name.</param>
/// <param name="VenueType">What kind of place this is, if stated.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="TimeZoneId">IANA time-zone id for the venue, if known.</param>
/// <param name="Address">Postal address and optional coordinates.</param>
/// <param name="Gates">The venue's physical entry points.</param>
/// <param name="Facilities">What the venue offers.</param>
public sealed record VenueResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? VenueType,
    string Status,
    string? TimeZoneId,
    VenueAddressResponse Address,
    IReadOnlyList<VenueGateResponse> Gates,
    IReadOnlyList<VenueFacilityResponse> Facilities);
