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
        string priceTier,
        decimal priceAmount,
        int capacity)
    {
        Id = id;
        SeatMapId = seatMapId;
        SectionName = sectionName;
        PriceTier = priceTier;
        PriceAmount = priceAmount;
        Capacity = capacity;
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

    /// <summary>Price tier name (e.g. <c>General</c>).</summary>
    public string PriceTier { get; private set; } = default!;

    /// <summary>Ticket price in the event's currency.</summary>
    public decimal PriceAmount { get; private set; }

    /// <summary>Total number of admissions sellable in this section.</summary>
    public int Capacity { get; private set; }
}
