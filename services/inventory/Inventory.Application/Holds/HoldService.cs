namespace Inventory.Application.Holds;

/// <summary>
/// Orchestrates the no-oversell hold path: the Redis fast gate, the Postgres optimistic update
/// (final authority), and the transactional outbox — all lean, no MediatR (ADR-0009).
/// </summary>
/// <param name="inventory">The inventory repository.</param>
/// <param name="holdStore">The Redis hold store (fast gate).</param>
/// <param name="events">The integration-event publisher (outbox).</param>
/// <param name="options">Hold options (TTL, per-hold seat cap).</param>
public sealed class HoldService(
    IInventoryRepository inventory,
    IHoldStore holdStore,
    IEventPublisher events,
    HoldOptions options)
{
    /// <summary>Places a hold over the requested seats.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="eventId">The event the seats belong to.</param>
    /// <param name="seatIds">The requested seat ids.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The place-hold result.</returns>
    public async Task<PlaceHoldResult> PlaceHoldAsync(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken cancellationToken)
    {
        var distinctSeatIds = seatIds.Distinct().ToList();
        if (distinctSeatIds.Count == 0)
        {
            return PlaceHoldResult.Failed(PlaceHoldOutcome.SeatNotFound);
        }

        var items = await inventory.GetItemsBySeatsAsync(eventId, distinctSeatIds, cancellationToken);
        if (items.Count != distinctSeatIds.Count)
        {
            return PlaceHoldResult.Failed(PlaceHoldOutcome.SeatNotFound);
        }

        var unavailable = items.FirstOrDefault(item => item.Status != InventoryStatus.Available);
        if (unavailable is not null)
        {
            return PlaceHoldResult.Conflict(unavailable.SeatId);
        }

        var holdId = Guid.CreateVersion7();

        // 1) Redis fast gate: atomic check-and-set across the seat keys.
        var gate = await holdStore.TryHoldAsync(eventId, holdId, distinctSeatIds, options.Ttl, cancellationToken);
        if (!gate.Success)
        {
            return PlaceHoldResult.Conflict(gate.ConflictSeatId);
        }

        // 2) Postgres is the final authority. Optimistic concurrency (Version) rejects a lost race.
        var expiresAt = DateTimeOffset.UtcNow.Add(options.Ttl);
        var ledger = new List<LedgerEntry>(items.Count);
        foreach (var item in items)
        {
            item.Hold();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Available, InventoryStatus.Held, "hold", holdId));
        }

        var hold = Hold.Create(tenantId, eventId, userId, expiresAt, items.Select(item => item.Id));
        inventory.AddHold(hold);
        inventory.AddLedgerEntries(ledger);

        events.Enqueue(new SeatHeld(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            tenantId,
            hold.Id,
            eventId,
            userId,
            expiresAt,
            distinctSeatIds));

        if (await inventory.TrySaveChangesAsync(cancellationToken))
        {
            return PlaceHoldResult.Held(hold.Id, expiresAt);
        }

        // Lost the race in Postgres — undo the Redis gate so the seats free up immediately.
        await holdStore.ReleaseAsync(eventId, holdId, distinctSeatIds, cancellationToken);
        return PlaceHoldResult.Conflict(null);
    }

    /// <summary>Releases an active hold owned by the given user.</summary>
    /// <param name="userId">The caller.</param>
    /// <param name="holdId">The hold to release.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The release outcome.</returns>
    public async Task<ReleaseHoldOutcome> ReleaseHoldAsync(
        Guid userId,
        Guid holdId,
        CancellationToken cancellationToken)
    {
        var hold = await inventory.GetHoldAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return ReleaseHoldOutcome.NotFound;
        }

        if (hold.UserId != userId)
        {
            return ReleaseHoldOutcome.Forbidden;
        }

        if (hold.Status != HoldStatus.Active)
        {
            return ReleaseHoldOutcome.NotActive;
        }

        return await ReleaseInternalAsync(hold, "release", cancellationToken)
            ? ReleaseHoldOutcome.Released
            : ReleaseHoldOutcome.Conflict;
    }

    /// <summary>
    /// Reclaims an expired hold (called by the reaper). Releases its seats regardless of owner;
    /// a no-op if the hold is already gone or no longer active.
    /// </summary>
    /// <param name="holdId">The hold to reap.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the hold was reclaimed.</returns>
    public async Task<bool> ReapHoldAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var hold = await inventory.GetHoldAsync(holdId, cancellationToken);
        if (hold is null || hold.Status != HoldStatus.Active)
        {
            return false;
        }

        return await ReleaseInternalAsync(hold, "reap", cancellationToken);
    }

    private async Task<bool> ReleaseInternalAsync(Hold hold, string cause, CancellationToken cancellationToken)
    {
        var itemIds = hold.Items.Select(item => item.InventoryItemId).ToList();
        var items = await inventory.GetItemsByIdsAsync(itemIds, cancellationToken);

        var ledger = new List<LedgerEntry>();
        foreach (var item in items.Where(item => item.Status == InventoryStatus.Held))
        {
            item.Release();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Held, InventoryStatus.Available, cause, hold.Id));
        }

        hold.Release();
        inventory.AddLedgerEntries(ledger);

        var seatIds = items.Select(item => item.SeatId).ToList();
        events.Enqueue(new SeatReleased(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            hold.TenantId,
            hold.Id,
            hold.EventId,
            seatIds));

        if (!await inventory.TrySaveChangesAsync(cancellationToken))
        {
            return false;
        }

        // Postgres committed (the authority); now clear the Redis gate.
        await holdStore.ReleaseAsync(hold.EventId, hold.Id, seatIds, cancellationToken);
        return true;
    }
}
