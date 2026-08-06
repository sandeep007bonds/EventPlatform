namespace Queue.Application.Abstractions;

/// <summary>The state of a queue session, as reported by <see cref="IQueueStore"/>.</summary>
public enum QueueSessionStatus
{
    /// <summary>The session is not known to the store — never joined, or its admission has since expired.</summary>
    NotFound,

    /// <summary>The session is waiting in line.</summary>
    Waiting,

    /// <summary>The session has been admitted and may proceed to hold a seat.</summary>
    Admitted,
}
