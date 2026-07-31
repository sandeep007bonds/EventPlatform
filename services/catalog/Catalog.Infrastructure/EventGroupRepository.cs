namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IEventGroupRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class EventGroupRepository(CatalogDbContext dbContext) : IEventGroupRepository
{
    /// <inheritdoc />
    public void Add(EventGroup eventGroup) => dbContext.EventGroups.Add(eventGroup);

    /// <inheritdoc />
    public Task<EventGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.EventGroups.Include(g => g.SocialLinks).FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<EventGroup> Items, int TotalCount)> ListForTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.EventGroups.AsNoTracking().Include(g => g.SocialLinks).Where(g => g.TenantId == tenantId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(g => g.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventGroup>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await dbContext.EventGroups
            .AsNoTracking()
            .Include(g => g.SocialLinks)
            .Where(g => ids.Contains(g.Id))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
