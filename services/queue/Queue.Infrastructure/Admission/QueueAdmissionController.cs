namespace Queue.Infrastructure.Admission;

/// <summary>
/// Background service that periodically promotes waiting sessions to admitted, for every event
/// with queueing enabled. Mirrors <c>ExpiredHoldReaper</c>'s shape (scoped-per-tick repository
/// read, outer try/catch-log-and-continue) — the reconciler-style background service pattern
/// already established for Inventory's hold expiry.
/// </summary>
/// <param name="scopeFactory">Factory used to resolve scoped services per tick.</param>
/// <param name="store">The waiting-room store.</param>
/// <param name="options">The admission controller options.</param>
/// <param name="logger">The logger.</param>
public sealed class QueueAdmissionController(
    IServiceScopeFactory scopeFactory,
    IQueueStore store,
    QueueAdmissionOptions options,
    ILogger<QueueAdmissionController> logger)
    : BackgroundService
{
    // Tracked in-process, not persisted — losing this on a restart just means the next tick treats
    // every enabled event as due, so at worst one event promotes slightly earlier than scheduled.
    // Not a correctness concern: ZPOPMIN's atomicity is what actually prevents oversell/double-promotion.
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastPromotedAt = new();

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.TickInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PromoteDueEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Queue admission pass failed; retrying on next tick.");
            }
        }
    }

    private async Task PromoteDueEventsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<QueueSettings> enabled;
        using (var scope = scopeFactory.CreateScope())
        {
            var settingsRepository = scope.ServiceProvider.GetRequiredService<IQueueSettingsRepository>();
            enabled = await settingsRepository.ListEnabledAsync(cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var settings in enabled)
        {
            var due = !_lastPromotedAt.TryGetValue(settings.EventId, out var lastPromotedAt)
                || now - lastPromotedAt >= TimeSpan.FromSeconds(settings.IntervalSeconds);

            if (!due)
            {
                continue;
            }

            var promoted = await store.PromoteBatchAsync(
                settings.EventId,
                settings.AdmissionRatePerInterval,
                TimeSpan.FromSeconds(settings.SessionTtlSeconds),
                cancellationToken);

            _lastPromotedAt[settings.EventId] = now;

            if (promoted.Count > 0)
            {
                logger.LogInformation(
                    "Admitted {Count} session(s) for event {EventId}.",
                    promoted.Count,
                    settings.EventId);
            }
        }
    }
}
