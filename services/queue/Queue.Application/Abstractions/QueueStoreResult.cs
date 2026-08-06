namespace Queue.Application.Abstractions;

/// <summary>The outcome of an <see cref="IQueueStore"/> enqueue/status read.</summary>
/// <param name="Status">Whether the session is unknown, waiting, or admitted.</param>
/// <param name="Position">
/// The session's current zero-based position among waiting sessions, if <see cref="Status"/> is
/// <see cref="QueueSessionStatus.Waiting"/>; otherwise <see langword="null"/>. Reflects the live
/// rank in the underlying sorted set, so it naturally decreases as sessions ahead are admitted.
/// </param>
public sealed record QueueStoreResult(QueueSessionStatus Status, int? Position);
