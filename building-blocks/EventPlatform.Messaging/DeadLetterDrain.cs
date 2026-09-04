namespace EventPlatform.Messaging;

/// <summary>
/// Records a message this service could not handle, so it stops being invisible.
/// </summary>
/// <remarks>
/// Idempotent on the originating message id: Dapr delivers at least once, and the drain is a
/// subscriber like any other, so the same dead letter can arrive twice. A message with no readable
/// envelope has no id to dedupe on and is recorded every time — noisier than the alternative, and
/// the alternative is dropping the only evidence of the worst-formed messages.
/// </remarks>
/// <param name="dbContext">The owning service's dead-letter-carrying context.</param>
/// <param name="logger">The logger, so a dead letter is loud as well as recorded.</param>
public sealed class DeadLetterDrain(IDeadLetterDbContext dbContext, ILogger<DeadLetterDrain> logger)
{
    /// <summary>Records an undeliverable message.</summary>
    /// <param name="topic">The topic it was published to, if known.</param>
    /// <param name="body">The message body exactly as it arrived.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the record is saved.</returns>
    public async Task RecordAsync(string? topic, JsonNode? body, CancellationToken cancellationToken)
    {
        var envelope = EventEnvelope.TryRead(body, out var read) && read is not null ? read : null;
        var messageId = envelope?.MessageId ?? Guid.Empty;

        if (messageId != Guid.Empty
            && await dbContext.DeadLetterMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken))
        {
            return;
        }

        var message = new DeadLetterMessage(
            messageId,
            topic ?? envelope?.EventType ?? "unknown",
            body?.ToJsonString() ?? string.Empty,
            envelope?.CorrelationId ?? Guid.Empty,
            envelope?.CausationId);

        dbContext.DeadLetterMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Error, not Warning: a dead letter means a message was lost to the business, and something
        // downstream — an unprovisioned performance, an unissued ticket — is now missing.
        logger.LogError(
            "Dead-lettered {Topic} message {MessageId} (correlation {CorrelationId}); recorded as {Id}.",
            message.Topic,
            message.MessageId,
            message.CorrelationId,
            message.Id);
    }
}
