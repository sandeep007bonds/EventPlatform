namespace Venues.Domain;

/// <summary>A single addressable seat, belonging to a <see cref="SeatRow"/>.</summary>
/// <remarks>
/// <b>A seat has no price.</b> That is the deliberate difference from the seat model this replaces.
/// A seat is a physical fact about a building and changes roughly never; a price is a commercial
/// decision that changes weekly, per event, per phase. Stamping the price onto tens of thousands of
/// seat rows meant a reprice had to rewrite them all, and any that were missed lied. Price belongs
/// to the ticket product the seat is sold as, one row per product, in Catalog.
/// <para>
/// For the same reason a seat has no availability. Whether <i>this</i> seat is free for <i>that</i>
/// performance is Inventory's answer, and it differs per session while the seat does not.
/// </para>
/// </remarks>
public sealed class Seat
{
    internal Seat(Guid id, Guid seatRowId, string number, SeatAttributes attributes, bool isSellable)
    {
        Id = id;
        SeatRowId = seatRowId;
        Number = number;
        Attributes = attributes;
        IsSellable = isSellable;
    }

    // Parameterless ctor for EF Core materialization.
    private Seat()
    {
    }

    /// <summary>Unique seat id (UUID v7 — time-sortable). Stable for the life of the map version.</summary>
    public Guid Id { get; private set; }

    /// <summary>The row this seat sits in.</summary>
    public Guid SeatRowId { get; private set; }

    /// <summary>
    /// Seat number within the row. A string, not an integer — real venues number seats <c>12A</c>,
    /// <c>B2</c> and <c>101</c>, and an integer column quietly makes those unrepresentable.
    /// </summary>
    public string Number { get; private set; } = default!;

    /// <summary>Physical properties buyers need disclosed.</summary>
    public SeatAttributes Attributes { get; private set; }

    /// <summary>
    /// Whether the seat can ever be sold. <see langword="false"/> covers permanently dead space —
    /// a camera position, a seat with no view at all. This is a property of the building, not a
    /// trading decision: holding seats back for a particular show is Inventory's blocking, which is
    /// per-session and reversible.
    /// </summary>
    public bool IsSellable { get; private set; }
}
