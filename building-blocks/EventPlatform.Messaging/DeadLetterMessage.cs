namespace EventPlatform.Messaging;

/// <summary>A message this service could not handle, kept so a person can find out why.</summary>
/// <remarks>
/// The counterpart to <see cref="OutboxMessage"/>: that one is what we sent, this one is what we
/// could not receive. Both are append-only records of the message plumbing, and both exist because
/// the alternative is a failure that only ever appears as log noise.
/// </remarks>
public sealed class DeadLetterMessage
{
    /// <summary>Records an undeliverable message.</summary>
    /// <param name="messageId">The originating message's id, from its envelope — the dedupe key.</param>
    /// <param name="topic">The topic it was published to.</param>
    /// <param name="payload">The message body as received, envelope and all.</param>
    /// <param name="correlationId">The chain of work it belonged to.</param>
    /// <param name="causationId">The message that caused it, if any.</param>
    public DeadLetterMessage(
        Guid messageId,
        string topic,
        string payload,
        Guid correlationId,
        Guid? causationId)
    {
        Id = Guid.CreateVersion7();
        MessageId = messageId;
        Topic = topic;
        Payload = payload;
        CorrelationId = correlationId;
        CausationId = causationId;
        DeadLetteredAt = DateTimeOffset.UtcNow;
    }

    // Parameterless ctor for EF Core materialization.
    private DeadLetterMessage()
    {
    }

    /// <summary>Unique id of this dead-letter row (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The originating message's id, read from its envelope.
    /// </summary>
    /// <remarks>
    /// <see cref="Guid.Empty"/> when the message carried no readable envelope — which is itself
    /// worth recording rather than rejecting, since a message malformed enough to lose its envelope
    /// is exactly the kind that ends up here.
    /// </remarks>
    public Guid MessageId { get; private set; }

    /// <summary>The topic the message was published to.</summary>
    public string Topic { get; private set; } = default!;

    /// <summary>
    /// The message body exactly as received, envelope included.
    /// </summary>
    /// <remarks>
    /// Stored verbatim rather than parsed into columns: whatever made this message unhandleable may
    /// well be in the part a parser would drop, and the point of the record is to be able to look at
    /// what actually arrived.
    /// </remarks>
    public string Payload { get; private set; } = default!;

    /// <summary>The chain of work the message belonged to, for joining it to what else happened.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>The message that caused it, if its envelope named one.</summary>
    public Guid? CausationId { get; private set; }

    /// <summary>When this service gave up on the message (UTC).</summary>
    public DateTimeOffset DeadLetteredAt { get; private set; }

    /// <summary>
    /// When an operator marked this as dealt with, or <see langword="null"/> while outstanding.
    /// </summary>
    /// <remarks>
    /// Resolved rather than deleted, so the record of a failure survives the fixing of it — an
    /// audit trail that can be emptied is not one.
    /// </remarks>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Marks the message as dealt with.</summary>
    /// <param name="at">When it was resolved (UTC).</param>
    public void Resolve(DateTimeOffset at) => ResolvedAt = at;
}
