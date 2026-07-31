namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IVenueRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class VenueRepository(CatalogDbContext dbContext) : IVenueRepository
{
    /// <inheritdoc />
    public void Add(Venue venue) => dbContext.Venues.Add(venue);

    /// <inheritdoc />
    public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Venue> Items, int TotalCount)> ListForTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Venues.AsNoTracking().Where(v => v.TenantId == tenantId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(v => v.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
