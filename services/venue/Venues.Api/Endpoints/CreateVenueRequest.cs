namespace Venues.Api.Endpoints;

/// <summary>Request body for creating a venue.</summary>
/// <param name="Name">Venue name.</param>
/// <param name="VenueType">What kind of place this is (e.g. <c>Stadium</c>, <c>Beach club</c>).</param>
/// <param name="Address">Postal address and optional coordinates.</param>
/// <param name="TimeZoneId">IANA time-zone id for the venue, if known.</param>
public sealed record CreateVenueRequest(
    string Name,
    string? VenueType,
    VenueAddressInput Address,
    string? TimeZoneId);
