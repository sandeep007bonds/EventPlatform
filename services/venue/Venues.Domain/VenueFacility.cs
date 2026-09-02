namespace Venues.Domain;

/// <summary>
/// Something a venue offers that a buyer may want to know about before booking — step-free access,
/// parking, a bar, a creche.
/// </summary>
/// <remarks>
/// Deliberately free-text rather than an enum. The set differs by venue kind (a beach club and a
/// cricket stadium share almost nothing), and an enum here would have to be extended by a code
/// change and a migration every time an organizer described their venue accurately.
/// </remarks>
public sealed class VenueFacility
{
    internal VenueFacility(Guid id, Guid venueId, string name, string? description)
    {
        Id = id;
        VenueId = venueId;
        Name = name;
        Description = description;
    }

    // Parameterless ctor for EF Core materialization.
    private VenueFacility()
    {
    }

    /// <summary>Unique facility id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The venue this facility belongs to.</summary>
    public Guid VenueId { get; private set; }

    /// <summary>Facility name (e.g. <c>Step-free access</c>).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Optional detail shown alongside the name.</summary>
    public string? Description { get; private set; }
}
