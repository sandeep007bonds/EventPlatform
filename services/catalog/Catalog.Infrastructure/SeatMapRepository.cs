namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="ISeatMapRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class SeatMapRepository(CatalogDbContext dbContext) : ISeatMapRepository
{
    // AsSplitQuery avoids the Seats × GeneralAdmissionSections cartesian product a single query
    // with two sibling collection Includes would otherwise produce. On the tracked read
    // (GetTrackedByEventIdAsync) this matters for correctness, not just performance: a section
    // edit (SeatMap.RemoveSection + AddReservedSection/AddGeneralAdmissionSection in one unit of
    // work) was hitting a spurious DbUpdateConcurrencyException ("0 rows affected") on the DELETE
    // for a just-loaded, genuinely-existing Seat row — traced to the cartesian-product single
    // query EF warns about (MultipleCollectionIncludeWarning) and resolved by split queries.
    /// <inheritdoc />
    public void Add(SeatMap seatMap) => dbContext.SeatMaps.Add(seatMap);

    /// <inheritdoc />
    public Task<SeatMap?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.SeatMaps
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Seats)
            .Include(m => m.GeneralAdmissionSections)
            .FirstOrDefaultAsync(m => m.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task<SeatMap?> GetTrackedByEventIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.SeatMaps
            .AsSplitQuery()
            .Include(m => m.Seats)
            .Include(m => m.GeneralAdmissionSections)
            .FirstOrDefaultAsync(m => m.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
