namespace Inventory.Application.Blocking;

/// <summary>
/// Organizer-initiated seat blocking (e.g. a kill or a restricted view) — separate from the
/// buyer-facing hold path, but sharing the same authority model: Postgres (with optimistic
/// concurrency) is the system of record, and the Redis fast gate is updated only after Postgres
/// commits, exactly like release and convert-to-sold.
/// </summary>
/// <param name="inventory">The inventory repository.</param>
/// <param name="holdStore">The Redis hold store (fast gate).</param>
/// <param name="events">The integration-event publisher (outbox).</param>
public sealed class SeatBlockingService(
    IInventoryRepository inventory,
    IHoldStore holdStore,
    IEventPublisher events)
{
    /// <summary>Blocks the requested seats, all-or-nothing.</summary>
    /// <param name="tenantId">The caller's tenant — seats belonging to another tenant are treated as not found.</param>
    /// <param name="eventSessionId">The performance the seats belong to.</param>
    /// <param name="seatIds">The seats to block.</param>
    /// <param name="reason">Optional organizer-supplied reason, carried on the published event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The block result.</returns>
    public async Task<BlockSeatsResult> BlockAsync(
        Guid tenantId,
        Guid eventSessionId,
        IReadOnlyList<Guid> seatIds,
        string? reason,
        CancellationToken cancellationToken)
    {
        var distinctSeatIds = seatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return BlockSeatsResult.SeatNotFound();
        }

        var items = await inventory.GetItemsBySeatsAsync(eventSessionId, distinctSeatIds, cancellationToken);
        if (items.Count != distinctSeatIds.Count || items.Any(item => item.TenantId != tenantId))
        {
            return BlockSeatsResult.SeatNotFound();
        }

        var unavailable = items.FirstOrDefault(item => item.Status != InventoryStatus.Available);
        if (unavailable is not null)
        {
            return BlockSeatsResult.Conflict(unavailable.SeatId);
        }

        var blockRef = Guid.CreateVersion7();
        var ledger = new List<LedgerEntry>(items.Count);
        foreach (var item in items)
        {
            item.Block();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Available, InventoryStatus.Blocked, "block", blockRef));
        }

        inventory.AddLedgerEntries(ledger);

        events.Enqueue(new SeatBlocked(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            tenantId,
            items[0].CatalogEventId,
            eventSessionId,
            distinctSeatIds,
            reason));

        if (!await inventory.TrySaveChangesAsync(cancellationToken))
        {
            return BlockSeatsResult.Conflict(null);
        }

        // Postgres committed (the authority); now reflect it in the Redis fast gate.
        await holdStore.BlockAsync(eventSessionId, distinctSeatIds, cancellationToken);
        return BlockSeatsResult.Blocked();
    }

    /// <summary>Unblocks the requested seats, all-or-nothing.</summary>
    /// <param name="tenantId">The caller's tenant — seats belonging to another tenant are treated as not found.</param>
    /// <param name="eventSessionId">The performance the seats belong to.</param>
    /// <param name="seatIds">The seats to unblock.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The unblock outcome.</returns>
    public async Task<UnblockSeatsOutcome> UnblockAsync(
        Guid tenantId,
        Guid eventSessionId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken cancellationToken)
    {
        var distinctSeatIds = seatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return UnblockSeatsOutcome.SeatNotFound;
        }

        var items = await inventory.GetItemsBySeatsAsync(eventSessionId, distinctSeatIds, cancellationToken);
        if (items.Count != distinctSeatIds.Count || items.Any(item => item.TenantId != tenantId))
        {
            return UnblockSeatsOutcome.SeatNotFound;
        }

        if (items.Any(item => item.Status != InventoryStatus.Blocked))
        {
            return UnblockSeatsOutcome.Conflict;
        }

        var blockRef = Guid.CreateVersion7();
        var ledger = new List<LedgerEntry>(items.Count);
        foreach (var item in items)
        {
            item.Unblock();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Blocked, InventoryStatus.Available, "unblock", blockRef));
        }

        inventory.AddLedgerEntries(ledger);

        events.Enqueue(new SeatUnblocked(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            tenantId,
            items[0].CatalogEventId,
            eventSessionId,
            distinctSeatIds));

        if (!await inventory.TrySaveChangesAsync(cancellationToken))
        {
            return UnblockSeatsOutcome.Conflict;
        }

        await holdStore.UnblockAsync(eventSessionId, distinctSeatIds, cancellationToken);
        return UnblockSeatsOutcome.Unblocked;
    }
}
