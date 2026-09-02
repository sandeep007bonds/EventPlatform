namespace Venues.Domain;

/// <summary>Where a <see cref="Venue"/> physically is — a postal address plus optional coordinates.</summary>
/// <remarks>
/// One value rather than eight loose parameters: eight adjacent strings in a signature is an
/// invitation to transpose two of them, and the compiler cannot help when they are all
/// <see cref="string"/>. Mapped as an owned type, so it is columns on <c>venues</c> rather than a
/// table of its own — an address has no identity or lifecycle apart from the venue it locates.
/// </remarks>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
public sealed record VenueAddress(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude);
