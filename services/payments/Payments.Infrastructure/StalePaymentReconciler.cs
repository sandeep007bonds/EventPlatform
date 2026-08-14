namespace Payments.Infrastructure;

/// <summary>
/// Background service that closes out payments nobody came back for.
/// <para>
/// The checkout saga learns a payment's outcome three ways — the buyer's browser, the provider's
/// webhook, or its own poll (ADR-0028) — but every one of them needs *something* still watching.
/// A buyer who authenticates and then loses their tab, on a machine no webhook can reach, is
/// watched by nothing: the saga times out, and the payment sits <c>Initiated</c> forever. If the
/// money actually moved, that is a charge with no order, no ticket, and no trace anyone would
/// notice. This sweep is what makes that recoverable.
/// </para>
/// <para>
/// It reuses the same reconciliation everything else does, so a payment found captured emits the
/// ordinary <c>PaymentCaptured</c> — which Ordering's existing subscriber already handles for a
/// saga that has finished, refunding the buyer rather than leaving them charged for seats they
/// never got. Nothing new had to be invented for the orphan case; it just needed someone to look.
/// </para>
/// </summary>
/// <param name="scopeFactory">Factory used to resolve scoped services per payment.</param>
/// <param name="options">The reconciler options.</param>
/// <param name="logger">The logger.</param>
internal sealed class StalePaymentReconciler(
    IServiceScopeFactory scopeFactory,
    PaymentReconcilerOptions options,
    ILogger<StalePaymentReconciler> logger)
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
                await ReconcileBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stale-payment reconciliation pass failed; retrying on next tick.");
            }
        }
    }

    private async Task ReconcileBatchAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> orderIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
            orderIds = await repository.GetStaleInitiatedOrderIdsAsync(
                DateTimeOffset.UtcNow - options.StaleAfter,
                options.BatchSize,
                cancellationToken);
        }

        if (orderIds.Count == 0)
        {
            return;
        }

        var captured = 0;
        var failed = 0;

        foreach (var orderId in orderIds)
        {
            // Each payment is its own unit of work, so one provider hiccup doesn't stall the rest.
            using var scope = scopeFactory.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<PaymentSyncService>();

            var result = await sync.SyncAsync(orderId, cancellationToken);
            if (result == PaymentSyncResult.Pending)
            {
                // The provider still shows it in flight this long after the hold expired, so the
                // buyer is not coming back. Cancel it rather than sweeping it again forever.
                result = await sync.AbandonAsync(orderId, cancellationToken);
            }

            if (result == PaymentSyncResult.Captured)
            {
                captured++;
            }
            else if (result == PaymentSyncResult.Failed)
            {
                failed++;
            }
        }

        if (captured > 0)
        {
            // Worth its own line at warning: money moved for an order whose saga had already given
            // up, which means a refund is now in flight. Rare, and someone should see it.
            logger.LogWarning(
                "Reconciled {Count} abandoned payment(s) that the provider had actually captured.",
                captured);
        }

        if (failed > 0)
        {
            logger.LogInformation("Closed out {Count} abandoned payment(s).", failed);
        }
    }
}
