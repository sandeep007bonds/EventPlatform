namespace Venues.Domain;

/// <summary>
/// A named block of reserved seating within a seat-map version — Lower Tier, Block 104, the
/// Balcony.
/// </summary>
/// <remarks>
/// <b>An entity, not a string.</b> Section used to be a name copied onto every seat, which made it
/// impossible to give a section anything of its own: no gate, no display order, no code stable
/// across a rename, and no place to hang the shape that draws it. Every "section" operation was a
/// string comparison across seat rows, and a rename was a bulk update that could half-fail.
/// <para>
/// Sections hold reserved seating only. Standing and unreserved capacity is an
/// <see cref="AdmissionArea"/> — a different thing with no seats to address, and modelling both
/// through one type with a discriminator meant half the fields were always meaningless.
/// </para>
/// </remarks>
public sealed class VenueSection
{
    private readonly List<SeatRow> _rows = new();

    internal VenueSection(Guid id, Guid seatMapVersionId, string code, string name, int displayOrder, Guid? gateId)
    {
        Id = id;
        SeatMapVersionId = seatMapVersionId;
        Code = code;
        Name = name;
        DisplayOrder = displayOrder;
        GateId = gateId;
    }

    // Parameterless ctor for EF Core materialization.
    private VenueSection()
    {
    }

    /// <summary>Unique section id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The seat-map version this section belongs to.</summary>
    public Guid SeatMapVersionId { get; private set; }

    /// <summary>
    /// Short stable code, unique within the version across sections and admission areas
    /// (e.g. <c>LT</c>, <c>B104</c>). Survives a rename, which is what makes it safe for another
    /// service to store.
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>Display name (e.g. <c>Lower Tier</c>).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Ordering when sections are listed. Lower sorts first.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// The <see cref="VenueGate"/> this section is entered through, if the venue routes it to one.
    /// <see langword="null"/> means any gate.
    /// </summary>
    public Guid? GateId { get; private set; }

    /// <summary>The rows in this section.</summary>
    public IReadOnlyCollection<SeatRow> Rows => _rows;

    /// <summary>Total seats in the section, sellable or not.</summary>
    public int SeatCount => _rows.Sum(r => r.Seats.Count);

    /// <summary>Seats that can ever be sold — what capacity means commercially.</summary>
    public int SellableSeatCount => _rows.Sum(r => r.Seats.Count(s => s.IsSellable));

    internal SeatRow AddRow(string label, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        var row = new SeatRow(Guid.CreateVersion7(), Id, label, displayOrder);
        _rows.Add(row);

        return row;
    }
}
