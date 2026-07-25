namespace Catalog.Domain;

/// <summary>A single addressable seat within a <see cref="SeatMap"/>.</summary>
public sealed class Seat
{
    internal Seat(
        Guid id,
        Guid seatMapId,
        string section,
        string priceTier,
        decimal priceAmount,
        string row,
        int number)
    {
        Id = id;
        SeatMapId = seatMapId;
        Section = section;
        PriceTier = priceTier;
        PriceAmount = priceAmount;
        Row = row;
        Number = number;
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

    /// <summary>Price tier name (e.g. <c>Gold</c>).</summary>
    public string PriceTier { get; private set; } = default!;

    /// <summary>Seat price in the event's currency.</summary>
    public decimal PriceAmount { get; private set; }

    /// <summary>Row label within the section (e.g. <c>A</c>).</summary>
    public string Row { get; private set; } = default!;

    /// <summary>Seat number within the row (1-based).</summary>
    public int Number { get; private set; }

    /// <summary>Human-readable label, e.g. <c>Lower Tier-A12</c>.</summary>
    public string Label => $"{Section}-{Row}{Number}";
}
