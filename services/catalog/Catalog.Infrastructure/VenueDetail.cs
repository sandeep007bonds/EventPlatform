namespace Catalog.Infrastructure;

/// <summary>The Venue service's <c>GET /v1/venues/{id}</c> response, as far as Catalog reads it.</summary>
/// <param name="Name">Venue name.</param>
/// <param name="TimeZoneId">The venue's IANA time zone, if it has one.</param>
/// <param name="Address">Postal address; only the city and country are read.</param>
internal sealed record VenueDetail(string Name, string? TimeZoneId, VenueDetailAddress Address);
