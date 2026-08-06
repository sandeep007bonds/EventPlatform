namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IEntryGateRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class EntryGateRepository(CatalogDbContext dbContext) : IEntryGateRepository
{
    /// <inheritdoc />
    public void Add(EntryGate entryGate) => dbContext.EntryGates.Add(entryGate);

    /// <inheritdoc />
    public Task<EntryGate?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.EntryGates.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntryGate>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.EntryGates
            .AsNoTracking()
            .Where(g => g.EventId == eventId)
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
