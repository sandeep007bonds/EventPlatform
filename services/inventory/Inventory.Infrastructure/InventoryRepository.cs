namespace Inventory.Infrastructure;

/// <summary>EF Core implementation of <see cref="IInventoryRepository"/>.</summary>
/// <param name="dbContext">The Inventory database context.</param>
internal sealed class InventoryRepository(InventoryDbContext dbContext) : IInventoryRepository
{
    /// <inheritdoc />
    public void AddRange(IEnumerable<InventoryItem> items) => dbContext.InventoryItems.AddRange(items);

    /// <inheritdoc />
    public Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.InventoryItems.AnyAsync(i => i.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.InventoryItems.CountAsync(i => i.EventId == eventId, cancellationToken);

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
}
