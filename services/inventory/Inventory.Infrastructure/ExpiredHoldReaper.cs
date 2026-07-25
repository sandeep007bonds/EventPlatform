namespace Inventory.Infrastructure;

/// <summary>
/// Background service that reclaims holds past their expiry. Redis TTL already lets the hold
/// marker lapse; this reaper is the reconciler that returns the seats to available in Postgres
/// (the authority) and clears the Redis seat keys, emitting <c>SeatReleased</c>.
/// </summary>
/// <param name="scopeFactory">Factory used to resolve scoped services per hold.</param>
/// <param name="options">The reaper options.</param>
/// <param name="logger">The logger.</param>
internal sealed class ExpiredHoldReaper(
    IServiceScopeFactory scopeFactory,
    HoldReaperOptions options,
    ILogger<ExpiredHoldReaper> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReapBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hold reaper pass failed; retrying on next tick.");
            }
        }
    }

    private async Task ReapBatchAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> expiredIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            expiredIds = await repository.GetExpiredActiveHoldIdsAsync(
                DateTimeOffset.UtcNow,
                options.BatchSize,
                cancellationToken);
        }

        if (expiredIds.Count == 0)
        {
            return;
        }

        var reaped = 0;
        foreach (var holdId in expiredIds)
        {
            // Each hold is its own unit of work, so one lost race doesn't block the rest.
            using var scope = scopeFactory.CreateScope();
            var holds = scope.ServiceProvider.GetRequiredService<HoldService>();
            if (await holds.ReapHoldAsync(holdId, cancellationToken))
            {
                reaped++;
            }
        }

        if (reaped > 0)
        {
            logger.LogInformation("Reaped {Count} expired hold(s).", reaped);
        }
    }
}
