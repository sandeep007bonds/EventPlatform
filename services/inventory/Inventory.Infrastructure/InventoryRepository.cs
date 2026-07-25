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
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
