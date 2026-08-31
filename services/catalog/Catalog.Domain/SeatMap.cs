namespace Catalog.Domain;

/// <summary>
/// Aggregate root describing the sellable inventory for an <see cref="Event"/> — a mix of
/// individually-addressable reserved seats and/or capacity-only general-admission sections. Built
/// once while the event is still a draft, then handed to Inventory (on publish) to generate
/// inventory.
/// </summary>
public sealed class SeatMap
{
    /// <summary>
    /// Minor units per major unit, for the obsolete <c>PriceAmount</c> columns only. The live price
    /// is <see cref="TicketType.PriceMinor"/>; this exists solely to keep those columns populated
    /// until they are dropped, and goes with them.
    /// </summary>
    private const decimal MinorUnitsPerMajor = 100m;

    private readonly List<Seat> _seats = new();
    private readonly List<GeneralAdmissionSection> _generalAdmissionSections = new();

    // Parameterless ctor for EF Core materialization.
    private SeatMap()
    {
    }

    private SeatMap(Guid id, Guid eventId, Guid tenantId, string name)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        Name = name;
    }

    /// <summary>Unique seat-map id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event this seat map belongs to (one seat map per event).</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Seat-map name (e.g. the venue configuration used).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>The reserved seats in this map.</summary>
    public IReadOnlyCollection<Seat> Seats => _seats;

    /// <summary>The general-admission sections in this map.</summary>
    public IReadOnlyCollection<GeneralAdmissionSection> GeneralAdmissionSections => _generalAdmissionSections;

    /// <summary>Total sellable capacity — reserved seats plus general-admission capacity.</summary>
    public int Capacity => _seats.Count + _generalAdmissionSections.Sum(s => s.Capacity);

    /// <summary>Creates an empty seat map for the given event.</summary>
    /// <param name="eventId">The event the seat map belongs to.</param>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="name">Seat-map name.</param>
    /// <returns>A new, empty <see cref="SeatMap"/>.</returns>
    public static SeatMap Create(Guid eventId, Guid tenantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SeatMap(Guid.CreateVersion7(), eventId, tenantId, name);
    }

    /// <summary>
    /// Adds a reserved (individually-seated) rectangular section, generating one seat per row ×
    /// seat position. Rows are labelled <c>A</c>, <c>B</c>, … and seats numbered from 1.
    /// </summary>
    /// <param name="section">Section name; must be unique within the map (across both reserved and general-admission sections).</param>
    /// <param name="ticketType">The ticket type these seats are sold as — supplies the name and price.</param>
    /// <param name="rows">Number of rows (positive).</param>
    /// <param name="seatsPerRow">Seats per row (positive).</param>
    /// <param name="entryGateId">
    /// The <see cref="EntryGate"/> this section is restricted to, if any — <see langword="null"/>
    /// means no restriction. Callers must have already validated the id belongs to the same event.
    /// </param>
    /// <exception cref="InvalidOperationException">A section with the same name already exists.</exception>
    public void AddReservedSection(
        string section,
        TicketType ticketType,
        int rows,
        int seatsPerRow,
        Guid? entryGateId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentNullException.ThrowIfNull(ticketType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seatsPerRow);
        EnsureSectionNameIsUnique(section);

        // The name and price are still written to each seat, but only so the columns stay populated
        // through the migration window — the read model projects both from the ticket type, which is
        // what makes a reprice take effect rather than leaving thousands of stale copies here.
        var priceAmount = ticketType.PriceMinor / MinorUnitsPerMajor;

        for (var r = 0; r < rows; r++)
        {
            var rowLabel = BuildRowLabel(r);
            for (var number = 1; number <= seatsPerRow; number++)
            {
                _seats.Add(new Seat(
                    Guid.CreateVersion7(),
                    Id,
                    section,
                    ticketType.Id,
                    ticketType.Name,
                    priceAmount,
                    rowLabel,
                    number,
                    entryGateId));
            }
        }
    }

    /// <summary>
    /// Adds a general-admission section — a named, priced capacity pool with no individual seat
    /// identity.
    /// </summary>
    /// <param name="section">Section name; must be unique within the map (across both reserved and general-admission sections).</param>
    /// <param name="ticketType">The ticket type this section is sold as — supplies the name and price.</param>
    /// <param name="capacity">Total number of admissions sellable in this section (positive).</param>
    /// <param name="entryGateId">
    /// The <see cref="EntryGate"/> this section is restricted to, if any — <see langword="null"/>
    /// means no restriction. Callers must have already validated the id belongs to the same event.
    /// </param>
    /// <exception cref="InvalidOperationException">A section with the same name already exists.</exception>
    public void AddGeneralAdmissionSection(
        string section,
        TicketType ticketType,
        int capacity,
        Guid? entryGateId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentNullException.ThrowIfNull(ticketType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        EnsureSectionNameIsUnique(section);

        _generalAdmissionSections.Add(new GeneralAdmissionSection(
            Guid.CreateVersion7(),
            Id,
            section,
            ticketType.Id,
            ticketType.Name,
            ticketType.PriceMinor / MinorUnitsPerMajor,
            capacity,
            entryGateId));
    }

    /// <summary>
    /// Removes a section (reserved or general-admission) by name — safe only while the event is
    /// still a draft, before Inventory has provisioned anything from this map (the caller enforces
    /// that; this method has no event-status awareness of its own). Editing a section is the
    /// caller's remove-then-<see cref="AddReservedSection"/>/<see cref="AddGeneralAdmissionSection"/>
    /// pair, not a separate in-place update — this regenerates fresh seat/section ids, which is
    /// harmless pre-publish since nothing outside Catalog references them yet.
    /// </summary>
    /// <param name="section">The section name to remove.</param>
    /// <exception cref="InvalidOperationException">No section with that name exists.</exception>
    public void RemoveSection(string section)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);

        var removedSeats = _seats.RemoveAll(s => string.Equals(s.Section, section, StringComparison.OrdinalIgnoreCase));
        var removedGa = _generalAdmissionSections.RemoveAll(s => string.Equals(s.SectionName, section, StringComparison.OrdinalIgnoreCase));

        if (removedSeats == 0 && removedGa == 0)
        {
            throw new InvalidOperationException($"No section named '{section}' exists in this seat map.");
        }
    }

    // Spreadsheet-column style labels: A..Z, then AA..AZ, BA.. (supports up to 702 rows).
    private static string BuildRowLabel(int index)
    {
        const int alphabet = 26;
        var first = index / alphabet;
        var secondChar = (char)('A' + (index % alphabet));

        return first == 0
            ? $"{secondChar}"
            : $"{(char)('A' + first - 1)}{secondChar}";
    }

    private void EnsureSectionNameIsUnique(string section)
    {
        var exists = _seats.Any(s => string.Equals(s.Section, section, StringComparison.OrdinalIgnoreCase))
            || _generalAdmissionSections.Any(s => string.Equals(s.SectionName, section, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException($"Section '{section}' already exists in the seat map.");
        }
    }
}
