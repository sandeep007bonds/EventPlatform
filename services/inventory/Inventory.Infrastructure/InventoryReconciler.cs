namespace Inventory.Infrastructure;

/// <summary>
/// Background service that rebuilds the Redis fast gate from Postgres (the authority) after Redis is
/// restarted or flushed. Redis holds only a sparse cache of seat status; if it loses that state,
/// every seat looks available and the fast gate would wrongly admit holds on seats that are really
/// held or sold. Postgres optimistic concurrency still prevents actual oversell, but the gate would
/// churn until corrected — this reconciler restores it.
/// </summary>
/// <remarks>
/// Detection is a sentinel key that a flush wipes. The rebuild only ever writes restrictions
/// (held/sold), never frees a seat, so it can never itself cause oversell even under concurrent
/// holds. It runs once on startup and then on the configured interval.
/// </remarks>
/// <param name="scopeFactory">Factory used to resolve scoped services.</param>
/// <param name="options">The reconciler options.</param>
/// <param name="logger">The logger.</param>
internal sealed class InventoryReconciler(
    IServiceScopeFactory scopeFactory,
    InventoryReconcilerOptions options,
    ILogger<InventoryReconciler> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reconcile promptly on startup so a service that comes up after a Redis flush warms the gate
        // immediately, then keep watching on the interval.
        await SafeReconcileAsync(stoppingToken);

        using var timer = new PeriodicTimer(options.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SafeReconcileAsync(stoppingToken);
        }
    }

    private async Task SafeReconcileAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ReconcileAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Inventory reconciliation pass failed; retrying on next tick.");
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IHoldStore>();

        if (await store.IsInitializedAsync(cancellationToken))
        {
            // Redis is warm — its state was not lost, so nothing to rebuild.
            return;
        }

        var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var eventIds = await repository.GetSessionIdsWithActiveInventoryAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var restored = 0;
        foreach (var eventSessionId in eventIds)
        {
            var states = await repository.GetReconciliationStateAsync(eventSessionId, cancellationToken);
            if (states.Count == 0)
            {
                continue;
            }

            await store.RestoreAsync(states, now, cancellationToken);
            restored += states.Count;
        }

        // Marking initialized last means an interrupted rebuild is simply retried on the next tick.
        await store.MarkInitializedAsync(cancellationToken);

        logger.LogInformation(
            "Inventory reconciler rebuilt the Redis fast gate from Postgres: {SeatCount} seat state(s) across {EventCount} event(s).",
            restored,
            eventIds.Count);
    }
}
