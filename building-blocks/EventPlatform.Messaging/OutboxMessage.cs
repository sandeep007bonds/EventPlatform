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
    public OutboxMessage(Guid id, string topic, string type, string payload, DateTimeOffset occurredAt)
    {
        Id = id;
        Topic = topic;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
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

    /// <summary>When the message was published, or <see langword="null"/> while pending.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Marks the message as published at the given time.</summary>
    /// <param name="at">The publish timestamp (UTC).</param>
    public void MarkPublished(DateTimeOffset at) => PublishedAt = at;
}
