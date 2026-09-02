namespace Venues.Domain;

/// <summary>A row of seats within a <see cref="VenueSection"/>.</summary>
/// <remarks>
/// A real entity rather than a label repeated on every seat. That repetition is what made a row
/// unaddressable: "close row F" had to be expressed as a query over seats, row ordering could not
/// be stored, and nothing stopped two rows in one section both calling themselves <c>F</c>.
/// </remarks>
public sealed class SeatRow
{
    private readonly List<Seat> _seats = new();

    internal SeatRow(Guid id, Guid venueSectionId, string label, int displayOrder)
    {
        Id = id;
        VenueSectionId = venueSectionId;
        Label = label;
        DisplayOrder = displayOrder;
    }

    // Parameterless ctor for EF Core materialization.
    private SeatRow()
    {
    }

    /// <summary>Unique row id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The section this row belongs to.</summary>
    public Guid VenueSectionId { get; private set; }

    /// <summary>Row label, unique within the section (e.g. <c>A</c>, <c>AA</c>, <c>12</c>).</summary>
    public string Label { get; private set; } = default!;

    /// <summary>Front-to-back ordering within the section. Lower sorts first.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>The seats in this row.</summary>
    public IReadOnlyCollection<Seat> Seats => _seats;

    internal Seat AddSeat(string number, SeatAttributes attributes, bool isSellable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var seat = new Seat(Guid.CreateVersion7(), Id, number, attributes, isSellable);
        _seats.Add(seat);

        return seat;
    }
}
