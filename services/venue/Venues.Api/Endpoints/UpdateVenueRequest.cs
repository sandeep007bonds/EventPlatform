namespace Venues.Api.Endpoints;

/// <summary>Request body for updating a venue's descriptive detail.</summary>
/// <param name="Name">Venue name.</param>
/// <param name="VenueType">What kind of place this is.</param>
/// <param name="Address">Postal address and optional coordinates.</param>
/// <param name="TimeZoneId">IANA time-zone id for the venue, if known.</param>
public sealed record UpdateVenueRequest(
    string Name,
    string? VenueType,
    VenueAddressInput Address,
    string? TimeZoneId);
