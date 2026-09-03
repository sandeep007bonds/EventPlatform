namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IEventRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class EventRepository(CatalogDbContext dbContext) : IEventRepository
{
    /// <inheritdoc />
    public void Add(Event @event) => dbContext.Events.Add(@event);

    /// <inheritdoc />
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Events
            .Include(e => e.SocialLinks)
            .Include(e => e.Sessions)
            .ThenInclude(s => s.Allocations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Event?> GetBySlugAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.Events
            .AsNoTracking()
            .Include(e => e.SocialLinks)
            .Include(e => e.Sessions)
            .ThenInclude(s => s.Allocations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Slug == slug, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> ListSlugsForStemAsync(string stem, CancellationToken cancellationToken)
    {
        // `EF.Functions.Like` with an escaped stem rather than `StartsWith`: a stem is generated
        // from `EventSlug.Basis`, so it can only contain [a-z0-9-] and needs no escaping today —
        // but a stem arriving from anywhere else with a `%` in it would otherwise match the whole
        // table and hand back a set that makes every candidate look taken.
        var pattern = stem.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        var slugs = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.Slug == stem || EF.Functions.Like(e.Slug, pattern + "-%", "\\"))
            .Select(e => e.Slug)
            .ToListAsync(cancellationToken);

        return slugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(
        Guid? callerTenantId,
        EventStatus? status,
        Guid? eventGroupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Sessions without their allocations: a list renders dates and venues, never which block
        // is sold as which ticket type. Pulling the allocations too would multiply the row count
        // for something no list has ever displayed.
        var visible = dbContext.Events
            .AsNoTracking()
            .Include(e => e.SocialLinks)
            .Include(e => e.Sessions)
            .AsSplitQuery()
            .Where(e =>
            e.Status != EventStatus.Draft || (callerTenantId != null && e.TenantId == callerTenantId));

        if (status is not null)
        {
            visible = visible.Where(e => e.Status == status);
        }

        if (eventGroupId is not null)
        {
            visible = visible.Where(e => e.EventGroupId == eventGroupId);
        }

        var totalCount = await visible.CountAsync(cancellationToken);
        var items = await visible
            .OrderBy(e => e.FirstSessionStartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> ListForTenantAsync(
        Guid tenantId,
        EventStatus? status,
        Guid? eventGroupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Events
            .AsNoTracking()
            .Include(e => e.SocialLinks)
            .Include(e => e.Sessions)
            .AsSplitQuery()
            .Where(e => e.TenantId == tenantId);

        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        if (eventGroupId is not null)
        {
            query = query.Where(e => e.EventGroupId == eventGroupId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.FirstSessionStartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Event>> ListLegsForEventGroupAsync(Guid eventGroupId, CancellationToken cancellationToken) =>
        await dbContext.Events
            .AsNoTracking()
            .Where(e => e.EventGroupId == eventGroupId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
