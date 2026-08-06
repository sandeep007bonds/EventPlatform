namespace Queue.Infrastructure;

/// <summary>EF Core implementation of <see cref="IQueueSettingsRepository"/>.</summary>
/// <param name="dbContext">The Queue database context.</param>
internal sealed class QueueSettingsRepository(QueueDbContext dbContext) : IQueueSettingsRepository
{
    /// <inheritdoc />
    public void Add(QueueSettings settings) => dbContext.QueueSettings.Add(settings);

    /// <inheritdoc />
    public Task<QueueSettings?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.QueueSettings.FirstOrDefaultAsync(s => s.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public Task<QueueSettings?> GetForTenantAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.QueueSettings.FirstOrDefaultAsync(s => s.EventId == eventId && s.TenantId == tenantId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.QueueSettings.AnyAsync(s => s.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueueSettings>> ListEnabledAsync(CancellationToken cancellationToken) =>
        await dbContext.QueueSettings.Where(s => s.Enabled).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
