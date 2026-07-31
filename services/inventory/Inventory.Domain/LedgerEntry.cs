namespace Inventory.Domain;

/// <summary>
/// An append-only, immutable record of an inventory status change (the audit trail behind
/// no-oversell) — one row per transition: hold, release, sold, reap. Covers either a single
/// reserved seat (<see cref="InventoryItemId"/>) or a quantity of a general-admission allocation
/// (<see cref="GeneralAdmissionAllocationId"/> + <see cref="Quantity"/>), never both.
/// </summary>
public sealed class LedgerEntry
{
    // Parameterless ctor for EF Core materialization.
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid? inventoryItemId,
        Guid? generalAdmissionAllocationId,
        int? quantity,
        InventoryStatus? fromStatus,
        InventoryStatus toStatus,
        string cause,
        Guid? refId)
    {
        InventoryItemId = inventoryItemId;
        GeneralAdmissionAllocationId = generalAdmissionAllocationId;
        Quantity = quantity;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Cause = cause;
        RefId = refId;
        At = DateTimeOffset.UtcNow;
    }

    /// <summary>Auto-incrementing ledger id (database-generated).</summary>
    public long Id { get; }

    /// <summary>The reserved-seat inventory item that changed, if this entry is seat-based.</summary>
    public Guid? InventoryItemId { get; private set; }

    /// <summary>The general-admission allocation that changed, if this entry is quantity-based.</summary>
    public Guid? GeneralAdmissionAllocationId { get; private set; }

    /// <summary>Number of admissions affected, if this entry is quantity-based.</summary>
    public int? Quantity { get; private set; }

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

    /// <summary>Records a status change for a reserved seat.</summary>
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

        return new LedgerEntry(inventoryItemId, null, null, fromStatus, toStatus, cause, refId);
    }

    /// <summary>Records a status change for a quantity of a general-admission allocation.</summary>
    /// <param name="generalAdmissionAllocationId">The allocation that changed.</param>
    /// <param name="quantity">Number of admissions affected.</param>
    /// <param name="fromStatus">Status before the change, if any.</param>
    /// <param name="toStatus">Status after the change.</param>
    /// <param name="cause">What caused the change.</param>
    /// <param name="refId">Related id (hold or order), if any.</param>
    /// <returns>A new <see cref="LedgerEntry"/>.</returns>
    public static LedgerEntry RecordGeneralAdmission(
        Guid generalAdmissionAllocationId,
        int quantity,
        InventoryStatus? fromStatus,
        InventoryStatus toStatus,
        string cause,
        Guid? refId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cause);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return new LedgerEntry(null, generalAdmissionAllocationId, quantity, fromStatus, toStatus, cause, refId);
    }
}
