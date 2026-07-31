namespace Catalog.Domain;

/// <summary>
/// A physical location an organizer holds events at. Owned by a single tenant (organizer) — not
/// shared across tenants in this pass, so two organizers using the same building each create
/// their own <see cref="Venue"/> row.
/// </summary>
public sealed class Venue
{
    // Parameterless ctor for EF Core materialization.
    private Venue()
    {
    }

    private Venue(
        Guid id,
        Guid tenantId,
        string name,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string? postalCode,
        string country,
        double? latitude,
        double? longitude,
        int? capacity)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        Capacity = capacity;
    }

    /// <summary>Unique venue id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Venue name.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Street address, line 1.</summary>
    public string AddressLine1 { get; private set; } = default!;

    /// <summary>Street address, line 2 (suite/unit/etc.), if any.</summary>
    public string? AddressLine2 { get; private set; }

    /// <summary>City.</summary>
    public string City { get; private set; } = default!;

    /// <summary>State/province/region, if applicable.</summary>
    public string? Region { get; private set; }

    /// <summary>Postal/ZIP code, if applicable.</summary>
    public string? PostalCode { get; private set; }

    /// <summary>ISO 3166-1 alpha-2 country code (e.g. <c>US</c>).</summary>
    public string Country { get; private set; } = default!;

    /// <summary>Latitude, for a map pin — not full geocoding integration.</summary>
    public double? Latitude { get; private set; }

    /// <summary>Longitude, for a map pin.</summary>
    public double? Longitude { get; private set; }

    /// <summary>
    /// Nominal/advertised venue capacity. Distinct from any one event's
    /// <c>SeatMap.Capacity</c> (the actual generated-seat count) — a venue can be registered
    /// before any event's seat map exists.
    /// </summary>
    public int? Capacity { get; private set; }

    /// <summary>Creates a new venue for the given tenant.</summary>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="name">Venue name.</param>
    /// <param name="addressLine1">Street address, line 1.</param>
    /// <param name="addressLine2">Street address, line 2, if any.</param>
    /// <param name="city">City.</param>
    /// <param name="region">State/province/region, if applicable.</param>
    /// <param name="postalCode">Postal/ZIP code, if applicable.</param>
    /// <param name="country">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="latitude">Latitude, if known.</param>
    /// <param name="longitude">Longitude, if known.</param>
    /// <param name="capacity">Nominal venue capacity, if known.</param>
    /// <returns>A new <see cref="Venue"/>.</returns>
    public static Venue Create(
        Guid tenantId,
        string name,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string? postalCode,
        string country,
        double? latitude,
        double? longitude,
        int? capacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        return new Venue(
            Guid.CreateVersion7(),
            tenantId,
            name,
            addressLine1,
            addressLine2,
            city,
            region,
            postalCode,
            country,
            latitude,
            longitude,
            capacity);
    }

    /// <summary>Updates the venue's details in place.</summary>
    /// <param name="name">Venue name.</param>
    /// <param name="addressLine1">Street address, line 1.</param>
    /// <param name="addressLine2">Street address, line 2, if any.</param>
    /// <param name="city">City.</param>
    /// <param name="region">State/province/region, if applicable.</param>
    /// <param name="postalCode">Postal/ZIP code, if applicable.</param>
    /// <param name="country">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="latitude">Latitude, if known.</param>
    /// <param name="longitude">Longitude, if known.</param>
    /// <param name="capacity">Nominal venue capacity, if known.</param>
    public void Update(
        string name,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string? postalCode,
        string country,
        double? latitude,
        double? longitude,
        int? capacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        Capacity = capacity;
    }
}
