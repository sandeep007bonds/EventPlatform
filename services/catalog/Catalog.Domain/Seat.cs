namespace Catalog.Domain;

/// <summary>A single addressable seat within a <see cref="SeatMap"/>.</summary>
public sealed class Seat
{
    internal Seat(
        Guid id,
        Guid seatMapId,
        string section,
        Guid ticketTypeId,
        string priceTier,
        decimal priceAmount,
        string row,
        int number,
        Guid? entryGateId)
    {
        Id = id;
        SeatMapId = seatMapId;
        Section = section;
        TicketTypeId = ticketTypeId;
        PriceTier = priceTier;
        PriceAmount = priceAmount;
        Row = row;
        Number = number;
        EntryGateId = entryGateId;
    }

    // Parameterless ctor for EF Core materialization.
    private Seat()
    {
    }

    /// <summary>Unique seat id (UUID v7 — time-sortable). Stable across services.</summary>
    public Guid Id { get; private set; }

    /// <summary>The seat map this seat belongs to.</summary>
    public Guid SeatMapId { get; private set; }

    /// <summary>Section name (e.g. <c>Lower Tier</c>).</summary>
    public string Section { get; private set; } = default!;

    /// <summary>The <see cref="TicketType"/> this seat is sold as — its name, price and rules.</summary>
    public Guid TicketTypeId { get; private set; }

    /// <summary>
    /// Price tier name, superseded by <see cref="TicketTypeId"/>.
    /// </summary>
    /// <remarks>
    /// Kept only so the migration that introduced ticket types did not have to drop columns in the
    /// same step it backfilled them. Nothing reads it: the seat-map read model projects the name
    /// and price from the referenced <see cref="TicketType"/>, so a rename or reprice takes effect
    /// immediately instead of leaving stale copies here. Dropped once Ordering and promo codes
    /// scope off the id.
    /// </remarks>
    public string PriceTier { get; private set; } = default!;

    /// <summary>
    /// Seat price, superseded by <see cref="TicketTypeId"/>.
    /// </summary>
    /// <remarks>
    /// Kept only so the migration that introduced ticket types did not have to drop columns in the
    /// same step it backfilled them. Nothing reads it: the seat-map read model projects the name
    /// and price from the referenced <see cref="TicketType"/>, so a rename or reprice takes effect
    /// immediately instead of leaving stale copies here. Dropped once Ordering and promo codes
    /// scope off the id.
    /// </remarks>
    public decimal PriceAmount { get; private set; }

    /// <summary>Row label within the section (e.g. <c>A</c>).</summary>
    public string Row { get; private set; } = default!;

    /// <summary>Seat number within the row (1-based).</summary>
    public int Number { get; private set; }

    /// <summary>
    /// The <see cref="EntryGate"/> this seat's section is restricted to, if any — set once when
    /// the section is defined; <see langword="null"/> means no restriction (any gate). See
    /// <see cref="SeatMap.AddReservedSection"/>.
    /// </summary>
    public Guid? EntryGateId { get; private set; }

    /// <summary>Human-readable label, e.g. <c>Lower Tier-A12</c>.</summary>
    public string Label => $"{Section}-{Row}{Number}";
}
