namespace Payments.Application.Charging;

/// <summary>
/// Reconciles a payment by *asking* the provider for its current state, rather than waiting to be
/// told via webhook. Same end result as <see cref="PaymentWebhookService"/> — the identical domain
/// transitions and the identical outbox events — just pull instead of push.
/// <para>
/// This is what lets checkout complete on a machine the provider can't call back (localhost, where
/// a webhook would need a forwarding tunnel), and it doubles as a backstop in any environment if a
/// webhook is ever dropped. Both paths are safe to run against the same payment: the transitions
/// are <c>TryMark*</c>, so whichever arrives second is a no-op and no event is emitted twice.
/// </para>
/// </summary>
/// <param name="payments">The payment repository.</param>
/// <param name="gateway">The payment gateway.</param>
/// <param name="events">The integration-event publisher (outbox).</param>
public sealed class PaymentSyncService(
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IEventPublisher events)
{
    /// <summary>Re-reads an order's payment from the provider and applies any resulting transition.</summary>
    /// <param name="orderId">The order whose payment to re-read.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The payment's state after reconciliation.</returns>
    public async Task<PaymentSyncResult> SyncAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await payments.GetLatestByOrderAsync(orderId, cancellationToken);
        if (payment is null)
        {
            return PaymentSyncResult.NotFound;
        }

        // Already terminal locally — nothing to ask the provider about.
        if (payment.Status == PaymentStatus.Captured)
        {
            return PaymentSyncResult.Captured;
        }

        if (payment.Status == PaymentStatus.Failed)
        {
            return PaymentSyncResult.Failed;
        }

        if (payment.ProviderReference is null)
        {
            return PaymentSyncResult.Pending;
        }

        var status = await gateway.GetStatusAsync(payment.ProviderReference, cancellationToken);
        switch (status)
        {
            case GatewayPaymentStatus.Captured when payment.TryMarkCaptured(payment.ProviderReference):
                events.Enqueue(new PaymentCaptured(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    payment.TenantId,
                    payment.Id,
                    payment.OrderId,
                    payment.AmountMinor,
                    payment.Currency,
                    payment.ProviderReference!));
                await payments.SaveChangesAsync(cancellationToken);
                return PaymentSyncResult.Captured;

            case GatewayPaymentStatus.Failed when payment.TryMarkFailed("payment_failed"):
                events.Enqueue(new PaymentFailed(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    payment.TenantId,
                    payment.Id,
                    payment.OrderId,
                    payment.FailureReason!));
                await payments.SaveChangesAsync(cancellationToken);
                return PaymentSyncResult.Failed;

            case GatewayPaymentStatus.Captured:
                return PaymentSyncResult.Captured;

            case GatewayPaymentStatus.Failed:
                return PaymentSyncResult.Failed;

            default:
                return PaymentSyncResult.Pending;
        }
    }
}
