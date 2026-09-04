namespace EventPlatform.Messaging;

/// <summary>A persisted integration event awaiting publication (a transactional-outbox row).</summary>
public sealed class OutboxMessage
{
    /// <summary>Creates an outbox message.</summary>
    /// <param name="id">Unique id; also the dedupe id for consumers.</param>
    /// <param name="topic">Pub/sub topic to publish to.</param>
    /// <param name="type">Fully-qualified CLR type name of the event.</param>
    /// <param name="payload">JSON-serialized event payload.</param>
    /// <param name="occurredAt">When the event occurred (UTC).</param>
    /// <param name="correlationId">The chain of work this event belongs to.</param>
    /// <param name="causationId">The message that caused it, if any.</param>
    /// <param name="eventVersion">The contract version of the event.</param>
    public OutboxMessage(
        Guid id,
        string topic,
        string type,
        string payload,
        DateTimeOffset occurredAt,
        Guid correlationId,
        Guid? causationId,
        int eventVersion)
    {
        Id = id;
        Topic = topic;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        CausationId = causationId;
        EventVersion = eventVersion;
    }

    // Parameterless ctor for EF Core materialization.
    private OutboxMessage()
    {
    }

    /// <summary>Unique id; also used as the dedupe id by consumers.</summary>
    public Guid Id { get; private set; }

    /// <summary>Pub/sub topic to publish to.</summary>
    public string Topic { get; private set; } = default!;

    /// <summary>Fully-qualified CLR type name of the event.</summary>
    public string Type { get; private set; } = default!;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; private set; } = default!;

    /// <summary>When the event occurred (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// The chain of work this event belongs to, shared by everything descending from one
    /// originating action across every service it touches.
    /// </summary>
    /// <remarks>
    /// Stored as a column rather than only travelling on the wire, and indexed: the question this
    /// answers — "show me everything that happened because of that checkout" — is asked of the
    /// database long after the trace has expired (PLAT-015).
    /// </remarks>
    public Guid CorrelationId { get; private set; }

    /// <summary>
    /// The message that directly caused this one, or <see langword="null"/> when a person or a
    /// timer started the chain. Walking these one hop at a time reconstructs the order of events;
    /// <see cref="CorrelationId"/> alone gives only the unordered set.
    /// </summary>
    public Guid? CausationId { get; private set; }

    /// <summary>The event contract's version, from <c>EventVersionAttribute</c>.</summary>
    public int EventVersion { get; private set; }

    /// <summary>When the message was published, or <see langword="null"/> while pending.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Marks the message as published at the given time.</summary>
    /// <param name="at">The publish timestamp (UTC).</param>
    public void MarkPublished(DateTimeOffset at) => PublishedAt = at;
}
