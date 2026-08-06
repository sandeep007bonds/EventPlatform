namespace Queue.Application.Abstractions;

/// <summary>
/// The Redis-backed waiting room: an atomic (Lua) FIFO sorted set per event, plus TTL-expiring
/// admission markers. Deliberately holds no Postgres-backed durability of its own — a queue
/// position is ephemeral (losing it on a Redis restart means "back of the line," not a lost sale),
/// unlike Ticketing's scan cache (ADR-0025), which durably persists because losing it would break
/// check-in. See ADR-0026.
/// </summary>
public interface IQueueStore
{
    /// <summary>
    /// Joins the waiting line for an event, or resumes an existing session's current position —
    /// idempotent by construction, so a page refresh never re-enqueues at the back. Already-admitted
    /// sessions report <see cref="QueueSessionStatus.Admitted"/> immediately.
    /// </summary>
    /// <param name="eventId">The event being queued for.</param>
    /// <param name="sessionId">The client-generated session id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The session's resulting status and position.</returns>
    Task<QueueStoreResult> EnqueueOrResumeAsync(Guid eventId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>Reads a session's current status without enqueuing it — used for polling.</summary>
    /// <param name="eventId">The event being queued for.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The session's current status and position.</returns>
    Task<QueueStoreResult> GetStatusAsync(Guid eventId, Guid sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Promotes up to <paramref name="count"/> of the longest-waiting sessions to admitted, each
    /// with an admission marker valid for <paramref name="sessionTtl"/>. Called by the admission
    /// controller — atomic per session via <c>ZPOPMIN</c>, so concurrent callers (e.g. multiple
    /// Queue.Api replicas) can never double-promote the same session.
    /// </summary>
    /// <param name="eventId">The event to promote sessions for.</param>
    /// <param name="count">The maximum number of sessions to promote this pass.</param>
    /// <param name="sessionTtl">How long the resulting admission stays valid.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The session ids that were promoted, in promotion order.</returns>
    Task<IReadOnlyList<Guid>> PromoteBatchAsync(Guid eventId, int count, TimeSpan sessionTtl, CancellationToken cancellationToken);
}
