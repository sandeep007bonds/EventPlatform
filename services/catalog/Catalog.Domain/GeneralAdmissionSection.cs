namespace Catalog.Domain;

/// <summary>
/// A general-admission section within a <see cref="SeatMap"/> — a named, priced capacity pool
/// with no individual seat identity, as an alternative to <see cref="Seat"/>-backed reserved
/// sections. A single seat map may mix both kinds (e.g. reserved orchestra seating plus a GA
/// standing area).
/// </summary>
public sealed class GeneralAdmissionSection
{
    internal GeneralAdmissionSection(
        Guid id,
        Guid seatMapId,
        string sectionName,
        Guid ticketTypeId,
        string priceTier,
        decimal priceAmount,
        int capacity,
        Guid? entryGateId)
    {
        Id = id;
        SeatMapId = seatMapId;
        SectionName = sectionName;
        TicketTypeId = ticketTypeId;
        PriceTier = priceTier;
        PriceAmount = priceAmount;
        Capacity = capacity;
        EntryGateId = entryGateId;
    }

    // Parameterless ctor for EF Core materialization.
    private GeneralAdmissionSection()
    {
    }

    /// <summary>Unique id (UUID v7 — time-sortable). Stable across services — Inventory references it directly.</summary>
    public Guid Id { get; private set; }

    /// <summary>The seat map this section belongs to.</summary>
    public Guid SeatMapId { get; private set; }

    /// <summary>Section name (e.g. <c>Lawn</c>, <c>Standing</c>).</summary>
    public string SectionName { get; private set; } = default!;

    /// <summary>The <see cref="TicketType"/> this section is sold as — its name, price and rules.</summary>
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
    /// Ticket price, superseded by <see cref="TicketTypeId"/>.
    /// </summary>
    /// <remarks>
    /// Kept only so the migration that introduced ticket types did not have to drop columns in the
    /// same step it backfilled them. Nothing reads it: the seat-map read model projects the name
    /// and price from the referenced <see cref="TicketType"/>, so a rename or reprice takes effect
    /// immediately instead of leaving stale copies here. Dropped once Ordering and promo codes
    /// scope off the id.
    /// </remarks>

    public decimal PriceAmount { get; private set; }

    /// <summary>Total number of admissions sellable in this section.</summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// The <see cref="EntryGate"/> this section is restricted to, if any — set once when the
    /// section is defined; <see langword="null"/> means no restriction (any gate). See
    /// <see cref="SeatMap.AddGeneralAdmissionSection"/>.
    /// </summary>
    public Guid? EntryGateId { get; private set; }
}
