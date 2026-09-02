namespace Venues.Infrastructure;

/// <summary>EF Core implementation of <see cref="ISeatMapRepository"/>.</summary>
/// <remarks>
/// Both loaders fetch the root and then the versions they need as a <b>second query</b>, letting
/// EF's relationship fixup attach them. The obvious alternative — a filtered
/// <c>Include(m =&gt; m.Versions.Where(...))</c> — forces every <c>ThenInclude</c> branch to repeat
/// the identical filter, and EF throws if two of them ever drift apart. Two queries say the same
/// thing without that trap, and cost one extra round trip on a request that is already loading
/// thousands of seats.
/// <para>
/// Both are therefore <b>tracked</b> queries: fixup only happens for tracked entities, so
/// <c>AsNoTracking</c> here would silently return a seat map with no versions on it.
/// </para>
/// </remarks>
/// <param name="dbContext">The Venue database context.</param>
internal sealed class SeatMapRepository(VenuesDbContext dbContext) : ISeatMapRepository
{
    /// <inheritdoc />
    public void Add(SeatMap seatMap) => dbContext.SeatMaps.Add(seatMap);

    /// <inheritdoc />
    public async Task<SeatMap?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var seatMap = await dbContext.SeatMaps.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (seatMap is null)
        {
            return null;
        }

        // Superseded versions are immutable and nothing here can touch them, so their seats are not
        // worth the megabytes. Skipping them is safe for numbering too: a superseded version is by
        // definition older than the published one, so the highest number is always still in view.
        await LoadVersionsAsync(
            dbContext.Set<SeatMapVersion>()
                .Where(v => v.SeatMapId == id && v.Status != SeatMapVersionStatus.Superseded),
            cancellationToken);

        return seatMap;
    }

    /// <inheritdoc />
    public async Task<SeatMap?> GetWithVersionAsync(
        Guid id,
        int? versionNumber,
        CancellationToken cancellationToken)
    {
        var seatMap = await dbContext.SeatMaps.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (seatMap is null)
        {
            return null;
        }

        await LoadVersionsAsync(
            dbContext.Set<SeatMapVersion>()
                .Where(v => v.SeatMapId == id)
                .Where(v => versionNumber != null
                    ? v.VersionNumber == versionNumber
                    : v.Status == SeatMapVersionStatus.Published),
            cancellationToken);

        return seatMap;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SeatMap>> ListForVenueAsync(Guid venueId, CancellationToken cancellationToken)
    {
        var seatMaps = await dbContext.SeatMaps
            .Where(m => m.VenueId == venueId)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        // Version rows only, no layout: the summary needs a count and whether a draft is open.
        var ids = seatMaps.Select(m => m.Id).ToList();
        await dbContext.Set<SeatMapVersion>()
            .Where(v => ids.Contains(v.SeatMapId))
            .LoadAsync(cancellationToken);

        return seatMaps;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static Task LoadVersionsAsync(IQueryable<SeatMapVersion> versions, CancellationToken cancellationToken) =>
        versions
            .Include(v => v.Sections)
            .ThenInclude(s => s.Rows)
            .ThenInclude(r => r.Seats)
            .Include(v => v.AdmissionAreas)
            .Include(v => v.Elements)
            .AsSplitQuery()
            .LoadAsync(cancellationToken);
}
