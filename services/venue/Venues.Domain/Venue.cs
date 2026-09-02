namespace Venues.Domain;

/// <summary>
/// Aggregate root for a physical place events happen in — a stadium, a theatre, a club, a stretch
/// of beach.
/// </summary>
/// <remarks>
/// A venue is <b>reusable</b>, and that is the whole point of it existing. Before this service, a
/// location was eight fields typed onto each event and a seating layout rebuilt from scratch every
/// time, so two shows at the same stadium shared nothing — not the address, not the gates, not the
/// map. Here the venue is defined once and its seat maps are versioned assets an event points at.
/// <para>
/// The venue owns <i>physical</i> facts only: where it is, how you get in, what it offers. What is
/// on sale, at what price, on which night is the event's business and lives in Catalog. Keeping
/// that line sharp is what lets one venue serve a hundred events without any of them being able to
/// change it for the others.
/// </para>
/// </remarks>
public sealed class Venue
{
    private readonly List<VenueGate> _gates = new();
    private readonly List<VenueFacility> _facilities = new();

    // Parameterless ctor for EF Core materialization.
    private Venue()
    {
    }

    private Venue(Guid id, Guid tenantId, string name, string? venueType, VenueAddress address, string? timeZoneId)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        VenueType = venueType;
        Address = address;
        TimeZoneId = timeZoneId;
        Status = VenueStatus.Draft;
    }

    /// <summary>Unique venue id (UUID v7 — time-sortable). Stable across services.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Venue name (e.g. <c>DY Patil Stadium</c>).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// What kind of place this is (e.g. <c>Stadium</c>, <c>Theatre</c>, <c>Beach club</c>).
    /// Free text on purpose — see <see cref="VenueFacility"/> for the same reasoning.
    /// </summary>
    public string? VenueType { get; private set; }

    /// <summary>Postal address and optional coordinates.</summary>
    public VenueAddress Address { get; private set; } = default!;

    /// <summary>
    /// IANA time-zone id for the venue (e.g. <c>Asia/Kolkata</c>), if known. Nothing here reads it:
    /// every stored instant is unambiguous already. It exists so a client can render an event's
    /// times in the <i>venue's</i> zone rather than the reader's — a 7pm Delhi show should not read
    /// as 1:30pm to a buyer in London. Validated where a time-zone database is available, not here:
    /// an invariant that varies by machine is not an invariant.
    /// </summary>
    public string? TimeZoneId { get; private set; }

    /// <summary>Lifecycle state.</summary>
    public VenueStatus Status { get; private set; }

    /// <summary>The venue's physical entry points.</summary>
    public IReadOnlyCollection<VenueGate> Gates => _gates;

    /// <summary>What the venue offers.</summary>
    public IReadOnlyCollection<VenueFacility> Facilities => _facilities;

    /// <summary>Creates a new venue in <see cref="VenueStatus.Draft"/>.</summary>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="name">Venue name.</param>
    /// <param name="venueType">What kind of place this is, if stated.</param>
    /// <param name="address">Postal address and optional coordinates.</param>
    /// <param name="timeZoneId">IANA time-zone id, if known.</param>
    /// <returns>The new venue.</returns>
    public static Venue Create(
        Guid tenantId,
        string name,
        string? venueType,
        VenueAddress address,
        string? timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(address);

        return new Venue(Guid.CreateVersion7(), tenantId, name, venueType, address, timeZoneId);
    }

    /// <summary>
    /// Updates the venue's descriptive detail. Editable at any status: correcting a misspelled
    /// street or adding coordinates changes nothing anybody bought.
    /// </summary>
    /// <param name="name">Venue name.</param>
    /// <param name="venueType">What kind of place this is, if stated.</param>
    /// <param name="address">Postal address and optional coordinates.</param>
    /// <param name="timeZoneId">IANA time-zone id, if known.</param>
    public void UpdateDetails(string name, string? venueType, VenueAddress address, string? timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(address);

        Name = name;
        VenueType = venueType;
        Address = address;
        TimeZoneId = timeZoneId;
    }

    /// <summary>Makes the venue selectable for new events.</summary>
    /// <exception cref="InvalidOperationException">The venue is archived.</exception>
    public void Activate()
    {
        if (Status == VenueStatus.Archived)
        {
            throw new InvalidOperationException("An archived venue cannot be reactivated; create a new one.");
        }

        Status = VenueStatus.Active;
    }

    /// <summary>
    /// Retires the venue. Existing events keep working — archiving only stops it being chosen for
    /// new ones.
    /// </summary>
    public void Archive() => Status = VenueStatus.Archived;

    /// <summary>Adds a physical entry point.</summary>
    /// <param name="code">Short stable code, unique within the venue.</param>
    /// <param name="name">Display name.</param>
    /// <returns>The new gate.</returns>
    /// <exception cref="InvalidOperationException">A gate with the same code already exists.</exception>
    public VenueGate AddGate(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_gates.Any(g => string.Equals(g.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Gate code '{code}' is already used at this venue.");
        }

        var gate = new VenueGate(Guid.CreateVersion7(), Id, code, name);
        _gates.Add(gate);

        return gate;
    }

    /// <summary>Adds a facility.</summary>
    /// <param name="name">Facility name.</param>
    /// <param name="description">Optional detail.</param>
    /// <returns>The new facility.</returns>
    public VenueFacility AddFacility(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var facility = new VenueFacility(Guid.CreateVersion7(), Id, name, description);
        _facilities.Add(facility);

        return facility;
    }

    /// <summary>
    /// Whether a gate id belongs to this venue and is in use — what a seat map has to ask before it
    /// may route a section through that gate.
    /// </summary>
    /// <param name="gateId">The gate id to check.</param>
    /// <returns><see langword="true"/> if the gate is this venue's and active.</returns>
    public bool HasActiveGate(Guid gateId) => _gates.Any(g => g.Id == gateId && g.IsActive);
}
