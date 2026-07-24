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
            var data = JsonSerializer.Deserialize<JsonElement>(message.Payload, SerializerOptions);

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
}
