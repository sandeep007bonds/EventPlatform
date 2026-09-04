namespace Inventory.Domain;

/// <summary>
/// The Inventory system-of-record for one sellable seat at one performance. Postgres is the
/// authority for availability; Redis is the fast gate on the hot path. <see cref="Version"/> is an
/// optimistic concurrency token so a lost race in Postgres is the final rejecter of oversell.
/// </summary>
/// <remarks>
/// Keyed by <b>performance</b>, not by event: seat A1 on Friday and seat A1 on Saturday are two
/// independent things to sell, and the unique index says so (ADR-0039). The seat id belongs to a
/// pinned Venue seat-map version, which is immutable — so what this row points at cannot move under
/// it.
/// </remarks>
public sealed class InventoryItem
{
    // Parameterless ctor for EF Core materialization.
    private InventoryItem()
    {
    }

    private InventoryItem(
        Guid id,
        Guid tenantId,
        Guid eventSessionId,
        Guid catalogEventId,
        Guid seatId,
        Guid ticketTypeId,
        long priceMinor,
        InventoryStatus status)
    {
        Id = id;
        TenantId = tenantId;
        EventSessionId = eventSessionId;
        CatalogEventId = catalogEventId;
        SeatId = seatId;
        TicketTypeId = ticketTypeId;
        PriceMinor = priceMinor;
        Status = status;
    }

    /// <summary>Unique inventory-item id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The performance this seat is sellable for.</summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>
    /// The event the performance belongs to. Denormalised on purpose: the per-buyer ticket limit is
    /// counted across the whole run, and without this every such check would be a join.
    /// </summary>
    public Guid CatalogEventId { get; private set; }

    /// <summary>The Venue seat id this item maps to (stable across services).</summary>
    public Guid SeatId { get; private set; }

    /// <summary>
    /// The Catalog ticket type this seat is sold as, resolved from the performance's allocation map.
    /// </summary>
    public Guid TicketTypeId { get; private set; }

    /// <summary>
    /// Price in minor currency units, snapshotted when the performance was published.
    /// </summary>
    /// <remarks>
    /// A copy, not a reference: it lets a hold be quoted without a call to Catalog. Ordering
    /// re-derives what is actually charged from the order's own snapshot, so nothing treats this as
    /// the live price.
    /// </remarks>
    public long PriceMinor { get; private set; }

    /// <summary>Current availability status.</summary>
    public InventoryStatus Status { get; private set; }

    /// <summary>Optimistic-concurrency token; incremented on every status change.</summary>
    public int Version { get; private set; }

    /// <summary>Creates an inventory item for a seat.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventSessionId">The performance.</param>
    /// <param name="catalogEventId">The event the performance belongs to.</param>
    /// <param name="seatId">The Venue seat id.</param>
    /// <param name="ticketTypeId">The ticket type this seat is sold as.</param>
    /// <param name="priceMinor">Price in minor units, at publish time.</param>
    /// <param name="sellable">
    /// Whether the seat can ever be sold. A Venue seat marked non-sellable — dead space, a camera
    /// position — is provisioned <see cref="InventoryStatus.Blocked"/> rather than skipped, so the
    /// map still renders complete and the seat is visibly unavailable instead of absent.
    /// </param>
    /// <returns>A new <see cref="InventoryItem"/>.</returns>
    public static InventoryItem Create(
        Guid tenantId,
        Guid eventSessionId,
        Guid catalogEventId,
        Guid seatId,
        Guid ticketTypeId,
        long priceMinor,
        bool sellable = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(priceMinor);

        return new InventoryItem(
            Guid.CreateVersion7(),
            tenantId,
            eventSessionId,
            catalogEventId,
            seatId,
            ticketTypeId,
            priceMinor,
            sellable ? InventoryStatus.Available : InventoryStatus.Blocked);
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

    /// <summary>
    /// Releases a sold seat back to available (a buyer-initiated cancellation/refund) — the
    /// reverse of <see cref="MarkSold"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The seat is not sold.</exception>
    public void ReleaseSold()
    {
        Transition(InventoryStatus.Sold, InventoryStatus.Available);
    }

    /// <summary>
    /// Blocks an available seat so it can't be held or sold (e.g. a kill or a restricted view).
    /// Only available seats can be blocked — a seat already in a buyer's hold isn't yanked out
    /// from under them.
    /// </summary>
    /// <exception cref="InvalidOperationException">The seat is not available.</exception>
    public void Block()
    {
        Transition(InventoryStatus.Available, InventoryStatus.Blocked);
    }

    /// <summary>Unblocks a seat, returning it to available.</summary>
    /// <exception cref="InvalidOperationException">The seat is not blocked.</exception>
    public void Unblock()
    {
        Transition(InventoryStatus.Blocked, InventoryStatus.Available);
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
