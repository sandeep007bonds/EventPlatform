namespace Inventory.Infrastructure;

/// <summary>EF Core implementation of <see cref="IInventoryRepository"/>.</summary>
/// <param name="dbContext">The Inventory database context.</param>
internal sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    /// <inheritdoc />
    public void AddRange(IEnumerable<InventoryItem> items) => dbContext.InventoryItems.AddRange(items);

    /// <inheritdoc />
    public Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.EventInventorySettings.AnyAsync(s => s.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.InventoryItems.CountAsync(i => i.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryItem>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.InventoryItems
            .AsNoTracking()
            .Where(i => i.EventId == eventId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryItem>> GetItemsBySeatsAsync(
        Guid eventId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken) =>
        await dbContext.InventoryItems
            .Where(i => i.EventId == eventId && seatIds.Contains(i.SeatId))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<InventoryItem>> GetItemsByIdsAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken) =>
        await dbContext.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Hold?> GetHoldAsync(Guid holdId, CancellationToken cancellationToken) =>
        dbContext.Holds
            .Include(h => h.Items)
            .Include(h => h.GeneralAdmissionItems)
            .FirstOrDefaultAsync(h => h.Id == holdId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetExpiredActiveHoldIdsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.Holds
            .Where(h => h.Status == HoldStatus.Active && h.ExpiresAt < now)
            .OrderBy(h => h.ExpiresAt)
            .Take(batchSize)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetEventIdsWithActiveInventoryAsync(CancellationToken cancellationToken) =>
        await dbContext.InventoryItems
            .Where(i => i.Status == InventoryStatus.Held || i.Status == InventoryStatus.Sold || i.Status == InventoryStatus.Blocked)
            .Select(i => i.EventId)
            .Distinct()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatReconciliationState>> GetReconciliationStateAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        // Sold seats are permanent — no hold, no TTL.
        var sold = await dbContext.InventoryItems
            .Where(i => i.EventId == eventId && i.Status == InventoryStatus.Sold)
            .Select(i => new SeatReconciliationState(eventId, i.SeatId, SeatCondition.Sold, null, null))
            .ToListAsync(cancellationToken);

        // Blocked seats are organizer-set and not time-limited — no hold, no TTL.
        var blocked = await dbContext.InventoryItems
            .Where(i => i.EventId == eventId && i.Status == InventoryStatus.Blocked)
            .Select(i => new SeatReconciliationState(eventId, i.SeatId, SeatCondition.Blocked, null, null))
            .ToListAsync(cancellationToken);

        // Held seats join through their active hold to recover the hold id and expiry (the TTL).
        var held = await (
            from item in dbContext.InventoryItems
            where item.EventId == eventId && item.Status == InventoryStatus.Held
            join holdItem in dbContext.Set<HoldItem>() on item.Id equals holdItem.InventoryItemId
            join hold in dbContext.Holds on holdItem.HoldId equals hold.Id
            where hold.Status == HoldStatus.Active
            select new SeatReconciliationState(eventId, item.SeatId, SeatCondition.Held, hold.Id, hold.ExpiresAt))
            .ToListAsync(cancellationToken);

        return [.. sold, .. blocked, .. held];
    }

    /// <inheritdoc />
    public void AddHold(Hold hold) => dbContext.Holds.Add(hold);

    /// <inheritdoc />
    public void AddLedgerEntries(IEnumerable<LedgerEntry> entries) => dbContext.LedgerEntries.AddRange(entries);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void AddGeneralAdmissionAllocations(IEnumerable<GeneralAdmissionAllocation> allocations) =>
        dbContext.GeneralAdmissionAllocations.AddRange(allocations);

    /// <inheritdoc />
    public Task<int> CountGeneralAdmissionAllocationsForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.GeneralAdmissionAllocations.CountAsync(a => a.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeneralAdmissionAllocation>> GetGeneralAdmissionAllocationsByIdsAsync(
        IReadOnlyCollection<Guid> allocationIds,
        CancellationToken cancellationToken) =>
        await dbContext.GeneralAdmissionAllocations
            .Where(a => allocationIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeneralAdmissionAllocation>> ListGeneralAdmissionForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.GeneralAdmissionAllocations
            .AsNoTracking()
            .Where(a => a.EventId == eventId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<EventInventorySettings?> GetEventInventorySettingsAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.EventInventorySettings.FirstOrDefaultAsync(s => s.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public void AddEventInventorySettings(EventInventorySettings settings) =>
        dbContext.EventInventorySettings.Add(settings);

    /// <inheritdoc />
    public async Task<int> GetBuyerCommittedQuantityAsync(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var seatCount = await (
            from holdItem in dbContext.Set<HoldItem>()
            join hold in dbContext.Holds on holdItem.HoldId equals hold.Id
            where hold.EventId == eventId
                && hold.UserId == userId
                && (hold.Status == HoldStatus.Active || hold.Status == HoldStatus.Converted)
            select holdItem)
            .CountAsync(cancellationToken);

        var gaQuantity = await (
            from gaItem in dbContext.Set<HoldGeneralAdmissionItem>()
            join hold in dbContext.Holds on gaItem.HoldId equals hold.Id
            where hold.EventId == eventId
                && hold.UserId == userId
                && (hold.Status == HoldStatus.Active || hold.Status == HoldStatus.Converted)
            select gaItem.Quantity)
            .SumAsync(cancellationToken);

        return seatCount + gaQuantity;
    }
}
