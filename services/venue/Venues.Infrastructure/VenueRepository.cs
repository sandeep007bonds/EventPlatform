namespace Venues.Infrastructure;

/// <summary>EF Core implementation of <see cref="IVenueRepository"/>.</summary>
/// <param name="dbContext">The Venue database context.</param>
internal sealed class VenueRepository(VenuesDbContext dbContext) : IVenueRepository
{
    /// <inheritdoc />
    public void Add(Venue venue) => dbContext.Venues.Add(venue);

    /// <inheritdoc />
    public Task<Venue?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Venues
            .Include(v => v.Gates)
            .Include(v => v.Facilities)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Venues
            .AsNoTracking()
            .Include(v => v.Gates)
            .Include(v => v.Facilities)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Venue>> ListForTenantAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken) =>
        await dbContext.Venues
            .AsNoTracking()
            .Include(v => v.Gates)
            .Where(v => v.TenantId == tenantId)
            .Where(v => includeArchived || v.Status != VenueStatus.Archived)
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
