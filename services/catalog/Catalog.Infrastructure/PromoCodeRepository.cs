namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IPromoCodeRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class PromoCodeRepository(CatalogDbContext dbContext) : IPromoCodeRepository
{
    /// <inheritdoc />
    public void Add(PromoCode promoCode) => dbContext.PromoCodes.Add(promoCode);

    /// <inheritdoc />
    public Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.PromoCodes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<PromoCode?> GetByCodeAsync(Guid eventId, string code, CancellationToken cancellationToken)
    {
        // Upper-cased here rather than in the query so Postgres can use the unique index on
        // (EventId, Code) — an `UPPER(code) = @p` predicate would not be sargable against it.
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();

        return dbContext.PromoCodes
            .AsNoTracking()
            .Include(p => p.Tiers)
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.Code == normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromoCode>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.PromoCodes
            .AsNoTracking()
            .Include(p => p.Tiers)
            .Where(p => p.EventId == eventId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
