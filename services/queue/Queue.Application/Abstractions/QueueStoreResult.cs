namespace Queue.Application.Abstractions;

/// <summary>The outcome of an <see cref="IQueueStore"/> enqueue/status read.</summary>
/// <param name="Status">Whether the session is unknown, waiting, or admitted.</param>
/// <param name="Position">
/// The session's current zero-based position among waiting sessions, if <see cref="Status"/> is
/// <see cref="QueueSessionStatus.Waiting"/>; otherwise <see langword="null"/>. Reflects the live
/// rank in the underlying sorted set, so it naturally decreases as sessions ahead are admitted.
/// </param>
/// <param name="WasCreated">
/// Whether this call added the session to the waiting set, as opposed to resuming one already
/// there. Only a creation costs a caller rate-limit budget: a buyer refreshing the waiting room
/// resumes the same session and should cost nothing, while a script minting fresh session ids
/// pays on every one (ADR-0026).
/// </param>
public sealed record QueueStoreResult(QueueSessionStatus Status, int? Position, bool WasCreated = false);
