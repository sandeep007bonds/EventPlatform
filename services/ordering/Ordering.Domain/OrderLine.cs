namespace Ordering.Domain;

/// <summary>
/// A single line within an <see cref="Order"/> — either a reserved seat
/// (<see cref="InventoryItemId"/>/<see cref="SeatId"/> set, <see cref="Quantity"/> always 1) or a
/// general-admission quantity (<see cref="GeneralAdmissionAllocationId"/> set), never both.
/// </summary>
public sealed class OrderLine
{
    internal OrderLine(Guid orderId, OrderLineSpec spec)
    {
        Id = Guid.CreateVersion7();
        OrderId = orderId;
        InventoryItemId = spec.InventoryItemId;
        SeatId = spec.SeatId;
        GeneralAdmissionAllocationId = spec.GeneralAdmissionAllocationId;
        Quantity = spec.Quantity;
        PriceTier = spec.PriceTier;
        UnitPriceMinor = spec.UnitPriceMinor;
        PriceMinor = spec.PriceMinor;
    }

    // Parameterless ctor for EF Core materialization.
    private OrderLine()
    {
    }

    /// <summary>Unique line id.</summary>
    public Guid Id { get; private set; }

    /// <summary>The order this line belongs to.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>The inventory item sold on this line, if this line is a reserved seat.</summary>
    public Guid? InventoryItemId { get; private set; }

    /// <summary>The Catalog seat id, if this line is a reserved seat.</summary>
    public Guid? SeatId { get; private set; }

    /// <summary>The general-admission allocation sold on this line, if this line is general admission.</summary>
    public Guid? GeneralAdmissionAllocationId { get; private set; }

    /// <summary>Number of admissions this line represents (1 for a reserved seat).</summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// The price tier this line was sold at (e.g. <c>"VIP"</c>), carried through from the hold.
    /// Decides whether a tier-restricted promo code discounts this line.
    /// </summary>
    public string PriceTier { get; private set; } = default!;

    /// <summary>Price per unit in minor currency units.</summary>
    public long UnitPriceMinor { get; private set; }

    /// <summary>Total price of this line in minor currency units.</summary>
    public long PriceMinor { get; private set; }
}
