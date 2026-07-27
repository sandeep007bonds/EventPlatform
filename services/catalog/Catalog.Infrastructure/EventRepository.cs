namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IEventRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class EventRepository(CatalogDbContext dbContext) : IEventRepository
{
    /// <inheritdoc />
    public void Add(Event @event) => dbContext.Events.Add(@event);

    /// <inheritdoc />
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(
        Guid? callerTenantId,
        EventStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var visible = dbContext.Events.AsNoTracking().Where(e =>
            e.Status != EventStatus.Draft || (callerTenantId != null && e.TenantId == callerTenantId));

        if (status is not null)
        {
            visible = visible.Where(e => e.Status == status);
        }

        var totalCount = await visible.CountAsync(cancellationToken);
        var items = await visible
            .OrderBy(e => e.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
