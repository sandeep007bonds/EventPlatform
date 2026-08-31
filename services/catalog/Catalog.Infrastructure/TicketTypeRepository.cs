namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="ITicketTypeRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class TicketTypeRepository(CatalogDbContext dbContext) : ITicketTypeRepository
{
    /// <inheritdoc />
    public void Add(TicketType ticketType) => dbContext.TicketTypes.Add(ticketType);

    /// <inheritdoc />
    public Task<TicketType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.TicketTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<TicketType?> GetByNameAsync(Guid eventId, string name, CancellationToken cancellationToken)
    {
        var normalized = (name ?? string.Empty).Trim();

        // Compared in memory over one event's types rather than as an `EF.Functions.ILike` predicate.
        // An event has a handful of ticket types, not thousands, so the index on EventId already
        // does the selective work — and a case-insensitive predicate could not use the unique index
        // on (EventId, Name) anyway, so pushing it down would buy an unindexed scan, not a saving.
        var candidates = await dbContext.TicketTypes
            .Where(t => t.EventId == eventId)
            .ToListAsync(cancellationToken);

        return candidates.Find(t => string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketType>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.TicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
