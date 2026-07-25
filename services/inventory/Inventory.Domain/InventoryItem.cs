namespace Inventory.Domain;

/// <summary>
/// The Inventory system-of-record for a single sellable seat. Postgres is the authority for
/// availability; Redis is the fast gate on the hot path. <see cref="Version"/> is an optimistic
/// concurrency token so a lost race in Postgres is the final rejecter of oversell.
/// </summary>
public sealed class InventoryItem
{
    // Parameterless ctor for EF Core materialization.
    private InventoryItem()
    {
    }

    private InventoryItem(Guid id, Guid tenantId, Guid eventId, Guid seatId, string priceTier, long priceMinor)
    {
        Id = id;
        TenantId = tenantId;
        EventId = eventId;
        SeatId = seatId;
        PriceTier = priceTier;
        PriceMinor = priceMinor;
        Status = InventoryStatus.Available;
    }

    /// <summary>Unique inventory-item id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The event this seat belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>The Catalog seat id this item maps to (stable across services).</summary>
    public Guid SeatId { get; private set; }

    /// <summary>Price tier name (from the Catalog seat map).</summary>
    public string PriceTier { get; private set; } = default!;

    /// <summary>Price in minor currency units (e.g. cents).</summary>
    public long PriceMinor { get; private set; }

    /// <summary>Current availability status.</summary>
    public InventoryStatus Status { get; private set; }

    /// <summary>Optimistic-concurrency token; incremented on every status change.</summary>
    public int Version { get; private set; }

    /// <summary>Creates an available inventory item for a seat.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventId">The event.</param>
    /// <param name="seatId">The Catalog seat id.</param>
    /// <param name="priceTier">Price tier name.</param>
    /// <param name="priceMinor">Price in minor units.</param>
    /// <returns>A new <see cref="InventoryItem"/> in <see cref="InventoryStatus.Available"/>.</returns>
    public static InventoryItem Create(Guid tenantId, Guid eventId, Guid seatId, string priceTier, long priceMinor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(priceTier);
        ArgumentOutOfRangeException.ThrowIfNegative(priceMinor);

        return new InventoryItem(Guid.CreateVersion7(), tenantId, eventId, seatId, priceTier, priceMinor);
    }

    /// <summary>Transitions an available seat to held.</summary>
    /// <exception cref="InvalidOperationException">The seat is not available.</exception>
    public void Hold()
    {
        Transition(InventoryStatus.Available, InventoryStatus.Held);
    }

    /// <summary>Releases a held seat back to available.</summary>
    /// <exception cref="InvalidOperationException">The seat is not held.</exception>
    public void Release()
    {
        Transition(InventoryStatus.Held, InventoryStatus.Available);
    }

    /// <summary>Converts a held seat to sold.</summary>
    /// <exception cref="InvalidOperationException">The seat is not held.</exception>
    public void MarkSold()
    {
        Transition(InventoryStatus.Held, InventoryStatus.Sold);
    }

    private void Transition(InventoryStatus from, InventoryStatus to)
    {
        if (Status != from)
        {
            throw new InvalidOperationException(
                $"Cannot move seat {SeatId} from {Status} to {to}; expected {from}.");
        }

        Status = to;
        Version++;
    }
}
