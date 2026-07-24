namespace Catalog.Domain;

/// <summary>
/// Aggregate root describing the sellable seats for an <see cref="Event"/>. Built once while the
/// event is still a draft, then handed to Inventory (on publish) to generate seat inventory.
/// </summary>
public sealed class SeatMap
{
    private readonly List<Seat> _seats = new();

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

    /// <summary>The seats in this map.</summary>
    public IReadOnlyCollection<Seat> Seats => _seats;

    /// <summary>Total number of seats.</summary>
    public int Capacity => _seats.Count;

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
    /// Adds a rectangular section, generating one seat per row × seat position. Rows are labelled
    /// <c>A</c>, <c>B</c>, … and seats numbered from 1.
    /// </summary>
    /// <param name="section">Section name; must be unique within the map.</param>
    /// <param name="priceTier">Price tier name for the section.</param>
    /// <param name="priceAmount">Seat price (non-negative) in the event's currency.</param>
    /// <param name="rows">Number of rows (positive).</param>
    /// <param name="seatsPerRow">Seats per row (positive).</param>
    /// <exception cref="InvalidOperationException">A section with the same name already exists.</exception>
    public void AddSection(string section, string priceTier, decimal priceAmount, int rows, int seatsPerRow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(priceTier);
        ArgumentOutOfRangeException.ThrowIfNegative(priceAmount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seatsPerRow);

        if (_seats.Any(s => string.Equals(s.Section, section, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Section '{section}' already exists in the seat map.");
        }

        for (var r = 0; r < rows; r++)
        {
            var rowLabel = BuildRowLabel(r);
            for (var number = 1; number <= seatsPerRow; number++)
            {
                _seats.Add(new Seat(Guid.CreateVersion7(), Id, section, priceTier, priceAmount, rowLabel, number));
            }
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
}
