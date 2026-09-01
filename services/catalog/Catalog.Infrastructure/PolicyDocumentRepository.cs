namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IPolicyDocumentRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class PolicyDocumentRepository(CatalogDbContext dbContext) : IPolicyDocumentRepository
{
    /// <inheritdoc />
    public void Add(PolicyDocument document) => dbContext.PolicyDocuments.Add(document);

    /// <inheritdoc />
    public Task<PolicyDocument?> GetAsync(Guid tenantId, Guid? eventId, PolicyKind kind, CancellationToken cancellationToken) =>
        dbContext.PolicyDocuments.FirstOrDefaultAsync(
            d => d.TenantId == tenantId && d.EventId == eventId && d.Kind == kind,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDocument>> ListDefaultsAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await dbContext.PolicyDocuments
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.EventId == null)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDocument>> ListForEventAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken) =>
        await dbContext.PolicyDocuments
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && (d.EventId == eventId || d.EventId == null))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
