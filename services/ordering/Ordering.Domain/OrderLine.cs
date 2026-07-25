namespace Ordering.Domain;

/// <summary>A single seat line within an <see cref="Order"/>.</summary>
public sealed class OrderLine
{
    internal OrderLine(Guid orderId, Guid inventoryItemId, Guid seatId, long priceMinor)
    {
        Id = Guid.CreateVersion7();
        OrderId = orderId;
        InventoryItemId = inventoryItemId;
        SeatId = seatId;
        PriceMinor = priceMinor;
    }

    // Parameterless ctor for EF Core materialization.
    private OrderLine()
    {
    }

    /// <summary>Unique line id.</summary>
    public Guid Id { get; private set; }

    /// <summary>The order this line belongs to.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>The inventory item sold on this line.</summary>
    public Guid InventoryItemId { get; private set; }

    /// <summary>The Catalog seat id.</summary>
    public Guid SeatId { get; private set; }

    /// <summary>Price in minor currency units.</summary>
    public long PriceMinor { get; private set; }
}
