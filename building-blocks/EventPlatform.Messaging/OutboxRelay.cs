namespace EventPlatform.Messaging;

/// <summary>
/// Background service that relays pending <see cref="OutboxMessage"/> rows to Dapr pub/sub.
/// Polls the outbox on an interval and publishes each unsent message at least once, marking it
/// published only after Dapr accepts it — so a crash mid-batch simply re-publishes next tick.
/// </summary>
/// <param name="scopeFactory">Factory used to resolve a scoped outbox DbContext per poll.</param>
/// <param name="daprClient">The Dapr client used to publish events.</param>
/// <param name="options">The relay options.</param>
/// <param name="logger">The logger.</param>
internal sealed class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    DaprClient daprClient,
    OutboxOptions options,
    ILogger<OutboxRelay> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RelayPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox relay poll failed; retrying on next tick.");
            }
        }
    }

    private async Task RelayPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxDbContext>();

        var pending = await dbContext.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.Id)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var message in pending)
        {
            var data = WithEnvelope(message);

            // Carry the outbox id as the CloudEvent id so consumers can dedupe on it.
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["cloudevent.id"] = message.Id.ToString(),
            };

            await daprClient.PublishEventAsync(
                options.PubSubName,
                message.Topic,
                data,
                metadata,
                cancellationToken);

            message.MarkPublished(DateTimeOffset.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Outbox relayed {Count} message(s) to pub/sub '{PubSub}'.",
            pending.Count,
            options.PubSubName);
    }

    /// <summary>Builds the message to publish: the stored event, with its delivery envelope.</summary>
    /// <remarks>
    /// Attached here rather than stored in the payload at enqueue time, because the envelope is
    /// about <i>delivery</i> and the payload is the domain event — keeping the stored row the plain
    /// event means it can still be replayed into a handler unchanged.
    /// </remarks>
    private static JsonNode WithEnvelope(OutboxMessage message)
    {
        // Parsed once and reused: the tenant is read off this same node rather than re-parsing,
        // because the relay does this for every message on every poll.
        var payload = JsonNode.Parse(message.Payload);

        var envelope = new EventEnvelope(
            message.Id,
            message.CorrelationId,
            message.CausationId,
            message.Topic,
            message.EventVersion,
            message.OccurredAt,
            TenantOf(payload));

        return envelope.AttachTo(payload);
    }

    // Read off the payload rather than duplicated onto the outbox row: the event already carries
    // it, and two copies of one fact is one chance for them to disagree.
    private static Guid TenantOf(JsonNode? payload) =>
        payload is JsonObject payloadObject
        && payloadObject.TryGetPropertyValue("tenantId", out var tenant)
        && Guid.TryParse(tenant?.GetValue<string>(), out var id)
            ? id
            : Guid.Empty;
}
