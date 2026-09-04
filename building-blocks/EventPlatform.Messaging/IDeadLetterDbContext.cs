namespace EventPlatform.Messaging;

/// <summary>
/// Persistence for the messages a service could not handle.
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="IOutboxDbContext"/>. The outbox is about <b>producing</b>
/// and this is about <b>consuming</b>, and the two sets of services are not the same: Communication
/// and Queue subscribe without ever publishing, so making them carry an outbox — and a relay
/// polling a table that is always empty — to get a dead-letter table would be paying for the wrong
/// thing.
/// </remarks>
public interface IDeadLetterDbContext
{
    /// <summary>Messages this service gave up on. See <see cref="DeadLetterMessage"/>.</summary>
    DbSet<DeadLetterMessage> DeadLetterMessages { get; }

    /// <summary>Persists pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
