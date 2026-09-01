namespace Catalog.Domain;

/// <summary>
/// Where an event happens — venue name plus a postal address and optional coordinates.
/// </summary>
/// <remarks>
/// Grouped into one value rather than passed as nine loose parameters, which is how they used to
/// travel. Nine adjacent strings in a signature is an invitation to transpose two of them, and the
/// compiler cannot help when they are all <see cref="string"/>.
/// <para>
/// Deliberately a value on <see cref="Event"/>, not a `Venue` entity. That is the right end state
/// and a separate piece of work — a venue is reusable across events and owns a seating layout,
/// which is a different lifecycle from an address typed once into a form.
/// </para>
/// </remarks>
/// <param name="Name">Location/venue name.</param>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
public sealed record EventLocation(
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude);
