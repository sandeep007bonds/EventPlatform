namespace Inventory.Domain;

/// <summary>
/// A buyer's temporary claim on inventory, subject to a TTL. Covers either or both individually
/// addressable seats (<see cref="HoldItem"/>) and general-admission quantities
/// (<see cref="HoldGeneralAdmissionItem"/>) — a real checkout may mix both, e.g. two reserved
/// seats plus three general-admission tickets in one purchase. Converted to a sale on checkout or
/// released (by the buyer or the expiry reaper).
/// </summary>
public sealed class Hold
{
    private readonly List<HoldItem> _items = new();
    private readonly List<HoldGeneralAdmissionItem> _generalAdmissionItems = new();

    // Parameterless ctor for EF Core materialization.
    private Hold()
    {
    }

    private Hold(Guid id, Guid tenantId, Guid eventId, Guid userId, DateTimeOffset expiresAt)
    {
        Id = id;
        TenantId = tenantId;
        EventId = eventId;
        UserId = userId;
        ExpiresAt = expiresAt;
        Status = HoldStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique hold id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The event the held inventory belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>The buyer holding the inventory.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The order this hold was converted for, once checkout starts.</summary>
    public Guid? OrderId { get; private set; }

    /// <summary>When the hold expires (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Current hold status.</summary>
    public HoldStatus Status { get; private set; }

    /// <summary>When the hold was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The individually-seated inventory items in this hold.</summary>
    public IReadOnlyCollection<HoldItem> Items => _items;

    /// <summary>The general-admission quantities in this hold.</summary>
    public IReadOnlyCollection<HoldGeneralAdmissionItem> GeneralAdmissionItems => _generalAdmissionItems;

    /// <summary>Creates an active hold over the given inventory items and/or general-admission quantities.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventId">The event.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="expiresAt">When the hold expires (UTC).</param>
    /// <param name="inventoryItemIds">The held inventory-item ids (reserved seats), if any.</param>
    /// <param name="generalAdmissionSelections">The held (allocation id, quantity) pairs (general admission), if any.</param>
    /// <returns>A new active <see cref="Hold"/>.</returns>
    /// <exception cref="InvalidOperationException">Neither collection contains any items.</exception>
    public static Hold Create(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        DateTimeOffset expiresAt,
        IEnumerable<Guid> inventoryItemIds,
        IEnumerable<(Guid AllocationId, int Quantity)> generalAdmissionSelections)
    {
        ArgumentNullException.ThrowIfNull(inventoryItemIds);
        ArgumentNullException.ThrowIfNull(generalAdmissionSelections);

        var hold = new Hold(Guid.CreateVersion7(), tenantId, eventId, userId, expiresAt);
        foreach (var itemId in inventoryItemIds)
        {
            hold._items.Add(new HoldItem(hold.Id, itemId));
        }

        foreach (var selection in generalAdmissionSelections)
        {
            hold._generalAdmissionItems.Add(new HoldGeneralAdmissionItem(hold.Id, selection.AllocationId, selection.Quantity));
        }

        if (hold._items.Count == 0 && hold._generalAdmissionItems.Count == 0)
        {
            throw new InvalidOperationException("A hold must contain at least one seat or general-admission quantity.");
        }

        return hold;
    }

    /// <summary>Marks the hold converted to a sale for the given order.</summary>
    /// <param name="orderId">The order the hold was converted for.</param>
    /// <exception cref="InvalidOperationException">The hold is not active.</exception>
    public void MarkConverted(Guid orderId)
    {
        RequireActive();
        OrderId = orderId;
        Status = HoldStatus.Converted;
    }

    /// <summary>Releases the hold.</summary>
    /// <exception cref="InvalidOperationException">The hold is not active.</exception>
    public void Release()
    {
        RequireActive();
        Status = HoldStatus.Released;
    }

    /// <summary>
    /// Extends the hold's expiry, e.g. once checkout submits and payment authentication begins.
    /// Only ever moves <see cref="ExpiresAt"/> forward — a replayed/retried call with an
    /// already-passed value is a safe no-op rather than a regression.
    /// </summary>
    /// <param name="newExpiresAt">The proposed new expiry (UTC).</param>
    /// <exception cref="InvalidOperationException">The hold is not active.</exception>
    public void Extend(DateTimeOffset newExpiresAt)
    {
        RequireActive();
        if (newExpiresAt > ExpiresAt)
        {
            ExpiresAt = newExpiresAt;
        }
    }

    /// <summary>Marks a converted (sold) hold cancelled — a buyer-initiated cancellation/refund.</summary>
    /// <exception cref="InvalidOperationException">The hold is not converted.</exception>
    public void MarkCancelled()
    {
        if (Status != HoldStatus.Converted)
        {
            throw new InvalidOperationException($"Hold {Id} is {Status}, not Converted.");
        }

        Status = HoldStatus.Cancelled;
    }

    private void RequireActive()
    {
        if (Status != HoldStatus.Active)
        {
            throw new InvalidOperationException($"Hold {Id} is {Status}, not Active.");
        }
    }
}
