namespace Inventory.Domain;

/// <summary>
/// An append-only, immutable record of an inventory status change (the audit trail behind
/// no-oversell). One row per transition: hold, release, sold, reap.
/// </summary>
public sealed class LedgerEntry
{
    // Parameterless ctor for EF Core materialization.
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid inventoryItemId,
        InventoryStatus? fromStatus,
        InventoryStatus toStatus,
        string cause,
        Guid? refId)
    {
        InventoryItemId = inventoryItemId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Cause = cause;
        RefId = refId;
        At = DateTimeOffset.UtcNow;
    }

    /// <summary>Auto-incrementing ledger id.</summary>
    public long Id { get; private set; }

    /// <summary>The inventory item that changed.</summary>
    public Guid InventoryItemId { get; private set; }

    /// <summary>Status before the change, if any.</summary>
    public InventoryStatus? FromStatus { get; private set; }

    /// <summary>Status after the change.</summary>
    public InventoryStatus ToStatus { get; private set; }

    /// <summary>What caused the change (e.g. <c>hold</c>, <c>release</c>, <c>sold</c>, <c>reap</c>).</summary>
    public string Cause { get; private set; } = default!;

    /// <summary>Related id (hold id or order id), if any.</summary>
    public Guid? RefId { get; private set; }

    /// <summary>When the change occurred (UTC).</summary>
    public DateTimeOffset At { get; private set; }

    /// <summary>Records a status change.</summary>
    /// <param name="inventoryItemId">The inventory item that changed.</param>
    /// <param name="fromStatus">Status before the change, if any.</param>
    /// <param name="toStatus">Status after the change.</param>
    /// <param name="cause">What caused the change.</param>
    /// <param name="refId">Related id (hold or order), if any.</param>
    /// <returns>A new <see cref="LedgerEntry"/>.</returns>
    public static LedgerEntry Record(
        Guid inventoryItemId,
        InventoryStatus? fromStatus,
        InventoryStatus toStatus,
        string cause,
        Guid? refId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cause);

        return new LedgerEntry(inventoryItemId, fromStatus, toStatus, cause, refId);
    }
}
