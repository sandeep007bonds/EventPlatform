namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="ISeatMapRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class SeatMapRepository(CatalogDbContext dbContext) : ISeatMapRepository
{
    /// <inheritdoc />
    public void Add(SeatMap seatMap) => dbContext.SeatMaps.Add(seatMap);

    /// <inheritdoc />
    public Task<SeatMap?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.SeatMaps
            .AsNoTracking()
            .Include(m => m.Seats)
            .FirstOrDefaultAsync(m => m.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
