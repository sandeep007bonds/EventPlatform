namespace Inventory.Application.Holds;

/// <summary>
/// Orchestrates the no-oversell hold path: the Redis fast gate, the Postgres optimistic update
/// (final authority), and the transactional outbox — all lean, no MediatR (ADR-0009). Covers both
/// individually addressable seats and general-admission quantities, either or both in one hold.
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
    /// <summary>Places a hold over the requested seats and/or general-admission quantities.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="eventId">The event the inventory belongs to.</param>
    /// <param name="seatIds">The requested seat ids, if any.</param>
    /// <param name="generalAdmissionSelections">The requested (allocation id, quantity) pairs, if any.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The place-hold result.</returns>
    public async Task<PlaceHoldResult> PlaceHoldAsync(
        Guid tenantId,
        Guid userId,
        Guid eventId,
        IReadOnlyList<Guid> seatIds,
        IReadOnlyList<(Guid AllocationId, int Quantity)> generalAdmissionSelections,
        CancellationToken cancellationToken)
    {
        var distinctSeatIds = seatIds.Distinct().ToList();
        var gaSelections = generalAdmissionSelections
            .GroupBy(selection => selection.AllocationId)
            .Select(group => (AllocationId: group.Key, Quantity: group.Sum(selection => selection.Quantity)))
            .Where(selection => selection.Quantity > 0)
            .ToList();

        if (distinctSeatIds.Count == 0 && gaSelections.Count == 0)
        {
            return PlaceHoldResult.Failed(PlaceHoldOutcome.SeatNotFound);
        }

        var settings = await inventory.GetEventInventorySettingsAsync(eventId, cancellationToken);
        if (settings?.OnSaleAt is { } onSaleAt && DateTimeOffset.UtcNow < onSaleAt)
        {
            return PlaceHoldResult.Failed(PlaceHoldOutcome.OnSaleNotStarted);
        }

        if (settings?.BookingEndsAt is { } bookingEndsAt && DateTimeOffset.UtcNow > bookingEndsAt)
        {
            return PlaceHoldResult.Failed(PlaceHoldOutcome.BookingWindowClosed);
        }

        if (settings?.MaxTicketsPerBuyer is { } maxTicketsPerBuyer)
        {
            var requestedQuantity = distinctSeatIds.Count + gaSelections.Sum(selection => selection.Quantity);
            var alreadyCommitted = await inventory.GetBuyerCommittedQuantityAsync(eventId, userId, cancellationToken);
            if (alreadyCommitted + requestedQuantity > maxTicketsPerBuyer)
            {
                return PlaceHoldResult.Failed(PlaceHoldOutcome.BuyerLimitExceeded);
            }
        }

        var items = new List<InventoryItem>();
        if (distinctSeatIds.Count > 0)
        {
            items = (await inventory.GetItemsBySeatsAsync(eventId, distinctSeatIds, cancellationToken)).ToList();
            if (items.Count != distinctSeatIds.Count)
            {
                return PlaceHoldResult.Failed(PlaceHoldOutcome.SeatNotFound);
            }

            var unavailable = items.FirstOrDefault(item => item.Status != InventoryStatus.Available);
            if (unavailable is not null)
            {
                return PlaceHoldResult.Conflict(unavailable.SeatId);
            }
        }

        var allocations = new List<GeneralAdmissionAllocation>();
        if (gaSelections.Count > 0)
        {
            var allocationIds = gaSelections.Select(selection => selection.AllocationId).ToList();
            allocations = (await inventory.GetGeneralAdmissionAllocationsByIdsAsync(allocationIds, cancellationToken)).ToList();
            if (allocations.Count != allocationIds.Count)
            {
                return PlaceHoldResult.Failed(PlaceHoldOutcome.AllocationNotFound);
            }

            var allocationsById = allocations.ToDictionary(allocation => allocation.Id);
            foreach (var selection in gaSelections)
            {
                var allocation = allocationsById[selection.AllocationId];
                if (selection.Quantity > allocation.RemainingCapacity)
                {
                    return PlaceHoldResult.Conflict(conflictSeatId: null, conflictAllocationId: allocation.Id);
                }
            }
        }

        var holdId = Guid.CreateVersion7();

        // 1) Redis fast gate: atomic check-and-set across the seat keys and/or GA capacity counters.
        if (distinctSeatIds.Count > 0)
        {
            var seatGate = await holdStore.TryHoldAsync(eventId, holdId, distinctSeatIds, options.Ttl, cancellationToken);
            if (!seatGate.Success)
            {
                return PlaceHoldResult.Conflict(seatGate.ConflictSeatId);
            }
        }

        if (gaSelections.Count > 0)
        {
            var gaGate = await holdStore.TryHoldGeneralAdmissionAsync(eventId, holdId, gaSelections, options.Ttl, cancellationToken);
            if (!gaGate.Success)
            {
                if (distinctSeatIds.Count > 0)
                {
                    await holdStore.ReleaseAsync(eventId, holdId, distinctSeatIds, cancellationToken);
                }

                return PlaceHoldResult.Conflict(conflictSeatId: null, conflictAllocationId: gaGate.ConflictAllocationId);
            }
        }

        // 2) Postgres is the final authority. Optimistic concurrency (Version) rejects a lost race.
        var expiresAt = DateTimeOffset.UtcNow.Add(options.Ttl);
        var ledger = new List<LedgerEntry>(items.Count + gaSelections.Count);
        foreach (var item in items)
        {
            item.Hold();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Available, InventoryStatus.Held, "hold", holdId));
        }

        foreach (var selection in gaSelections)
        {
            var allocation = allocations.First(a => a.Id == selection.AllocationId);
            allocation.Hold(selection.Quantity);
            ledger.Add(LedgerEntry.RecordGeneralAdmission(
                allocation.Id, selection.Quantity, InventoryStatus.Available, InventoryStatus.Held, "hold", holdId));
        }

        var hold = Hold.Create(
            tenantId,
            eventId,
            userId,
            expiresAt,
            items.Select(item => item.Id),
            gaSelections.Select(selection => (selection.AllocationId, selection.Quantity)));
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

        // Lost the race in Postgres — undo the Redis gate so the seats/capacity free up immediately.
        if (distinctSeatIds.Count > 0)
        {
            await holdStore.ReleaseAsync(eventId, holdId, distinctSeatIds, cancellationToken);
        }

        if (gaSelections.Count > 0)
        {
            await holdStore.ReleaseGeneralAdmissionAsync(eventId, holdId, gaSelections, cancellationToken);
        }

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

    /// <summary>Reads a hold (with priced lines) for validation and order pricing.</summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The hold view, or <see langword="null"/> if it does not exist.</returns>
    public async Task<HoldView?> GetHoldViewAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var hold = await inventory.GetHoldAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return null;
        }

        var itemIds = hold.Items.Select(item => item.InventoryItemId).ToList();
        var items = await inventory.GetItemsByIdsAsync(itemIds, cancellationToken);

        var allocationIds = hold.GeneralAdmissionItems.Select(item => item.GeneralAdmissionAllocationId).ToList();
        var allocations = await inventory.GetGeneralAdmissionAllocationsByIdsAsync(allocationIds, cancellationToken);
        var allocationsById = allocations.ToDictionary(allocation => allocation.Id);

        var lines = new List<HoldLineView>();
        lines.AddRange(items.Select(item =>
            new HoldLineView(item.Id, item.SeatId, null, 1, item.PriceTier, item.PriceMinor, item.PriceMinor)));
        lines.AddRange(hold.GeneralAdmissionItems.Select(gaItem =>
        {
            var allocation = allocationsById[gaItem.GeneralAdmissionAllocationId];
            return new HoldLineView(
                null,
                null,
                allocation.Id,
                gaItem.Quantity,
                allocation.PriceTier,
                allocation.PriceMinor,
                allocation.PriceMinor * gaItem.Quantity);
        }));

        return new HoldView(
            hold.Id,
            hold.TenantId,
            hold.EventId,
            hold.UserId,
            hold.Status.ToString(),
            hold.ExpiresAt,
            lines.Sum(line => line.PriceMinor),
            lines);
    }

    /// <summary>
    /// Converts an active hold to a sale for the given order. Idempotent: converting an already
    /// converted hold for the same order succeeds without change.
    /// </summary>
    /// <param name="holdId">The hold to convert.</param>
    /// <param name="orderId">The order the hold is being converted for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The conversion outcome.</returns>
    public async Task<ConvertHoldOutcome> ConvertToSoldAsync(
        Guid holdId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var hold = await inventory.GetHoldAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return ConvertHoldOutcome.NotFound;
        }

        if (hold.Status == HoldStatus.Converted)
        {
            return hold.OrderId == orderId ? ConvertHoldOutcome.Converted : ConvertHoldOutcome.NotActive;
        }

        if (hold.Status != HoldStatus.Active)
        {
            return ConvertHoldOutcome.NotActive;
        }

        if (hold.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return ConvertHoldOutcome.Expired;
        }

        var itemIds = hold.Items.Select(item => item.InventoryItemId).ToList();
        var items = await inventory.GetItemsByIdsAsync(itemIds, cancellationToken);

        var allocationIds = hold.GeneralAdmissionItems.Select(item => item.GeneralAdmissionAllocationId).ToList();
        var allocations = await inventory.GetGeneralAdmissionAllocationsByIdsAsync(allocationIds, cancellationToken);
        var allocationsById = allocations.ToDictionary(allocation => allocation.Id);

        var ledger = new List<LedgerEntry>();
        foreach (var item in items.Where(item => item.Status == InventoryStatus.Held))
        {
            item.MarkSold();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Held, InventoryStatus.Sold, "sold", orderId));
        }

        foreach (var gaItem in hold.GeneralAdmissionItems)
        {
            var allocation = allocationsById[gaItem.GeneralAdmissionAllocationId];
            allocation.MarkSold(gaItem.Quantity);
            ledger.Add(LedgerEntry.RecordGeneralAdmission(
                allocation.Id, gaItem.Quantity, InventoryStatus.Held, InventoryStatus.Sold, "sold", orderId));
        }

        hold.MarkConverted(orderId);
        inventory.AddLedgerEntries(ledger);

        var seatIds = items.Select(item => item.SeatId).ToList();
        events.Enqueue(new SeatSold(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            hold.TenantId,
            hold.Id,
            hold.EventId,
            orderId,
            seatIds));

        if (!await inventory.TrySaveChangesAsync(cancellationToken))
        {
            return ConvertHoldOutcome.Conflict;
        }

        if (seatIds.Count > 0)
        {
            await holdStore.MarkSoldAsync(hold.EventId, holdId, seatIds, cancellationToken);
        }

        if (hold.GeneralAdmissionItems.Count > 0)
        {
            await holdStore.MarkGeneralAdmissionSoldAsync(hold.EventId, holdId, cancellationToken);
        }

        return ConvertHoldOutcome.Converted;
    }

    /// <summary>
    /// Reclaims an expired hold (called by the reaper). Releases its seats/quantities regardless of
    /// owner; a no-op if the hold is already gone or no longer active.
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

    /// <summary>
    /// Releases an active hold on behalf of the system (checkout-saga compensation) — no owner
    /// check. A no-op if the hold is missing or no longer active.
    /// </summary>
    /// <param name="holdId">The hold to release.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the hold was released.</returns>
    public async Task<bool> SystemReleaseAsync(Guid holdId, CancellationToken cancellationToken)
    {
        var hold = await inventory.GetHoldAsync(holdId, cancellationToken);
        if (hold is null || hold.Status != HoldStatus.Active)
        {
            return false;
        }

        return await ReleaseInternalAsync(hold, "release", cancellationToken);
    }

    private async Task<bool> ReleaseInternalAsync(Hold hold, string cause, CancellationToken cancellationToken)
    {
        var itemIds = hold.Items.Select(item => item.InventoryItemId).ToList();
        var items = await inventory.GetItemsByIdsAsync(itemIds, cancellationToken);

        var allocationIds = hold.GeneralAdmissionItems.Select(item => item.GeneralAdmissionAllocationId).ToList();
        var allocations = await inventory.GetGeneralAdmissionAllocationsByIdsAsync(allocationIds, cancellationToken);
        var allocationsById = allocations.ToDictionary(allocation => allocation.Id);

        var ledger = new List<LedgerEntry>();
        foreach (var item in items.Where(item => item.Status == InventoryStatus.Held))
        {
            item.Release();
            ledger.Add(LedgerEntry.Record(item.Id, InventoryStatus.Held, InventoryStatus.Available, cause, hold.Id));
        }

        var gaReleases = new List<(Guid AllocationId, int Quantity)>();
        foreach (var gaItem in hold.GeneralAdmissionItems)
        {
            var allocation = allocationsById[gaItem.GeneralAdmissionAllocationId];
            allocation.Release(gaItem.Quantity);
            gaReleases.Add((allocation.Id, gaItem.Quantity));
            ledger.Add(LedgerEntry.RecordGeneralAdmission(
                allocation.Id, gaItem.Quantity, InventoryStatus.Held, InventoryStatus.Available, cause, hold.Id));
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
        if (seatIds.Count > 0)
        {
            await holdStore.ReleaseAsync(hold.EventId, hold.Id, seatIds, cancellationToken);
        }

        if (gaReleases.Count > 0)
        {
            await holdStore.ReleaseGeneralAdmissionAsync(hold.EventId, hold.Id, gaReleases, cancellationToken);
        }

        return true;
    }
}
