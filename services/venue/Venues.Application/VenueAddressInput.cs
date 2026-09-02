namespace Venues.Application;

/// <summary>A venue's postal address as supplied by a caller.</summary>
/// <remarks>
/// Separate from <see cref="VenueAddressResponse"/> even though the fields match today. They answer
/// to different pressures — what a caller may set versus what the API discloses — and collapsing
/// them means the first field that belongs to only one of those has nowhere to go.
/// </remarks>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
public sealed record VenueAddressInput(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude);
