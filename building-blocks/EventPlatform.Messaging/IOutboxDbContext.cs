namespace EventPlatform.Messaging;

/// <summary>A DbContext that owns the transactional outbox table.</summary>
public interface IOutboxDbContext
{
    /// <summary>The outbox messages.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>Saves pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
