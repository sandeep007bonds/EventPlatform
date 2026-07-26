namespace Payments.Application.Charging;

/// <summary>
/// Reconciles a payment against an out-of-band provider webhook (e.g. asynchronous capture or 3-D
/// Secure completion), idempotently. Delivery is at-least-once, so every event is deduped on its
/// provider id and every state transition is a no-op once already applied.
/// </summary>
/// <param name="payments">The payment repository.</param>
/// <param name="events">The integration-event publisher (outbox).</param>
public sealed class PaymentWebhookService(IPaymentRepository payments, IEventPublisher events)
{
    /// <summary>Processes a verified, provider-neutral webhook notification.</summary>
    /// <param name="notification">The verified notification.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the notification has been processed and recorded.</returns>
    public async Task ProcessAsync(PaymentWebhookNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Dedupe: a redelivered event must not re-apply a transition or re-emit an outbox message.
        if (await payments.HasProcessedWebhookAsync(notification.EventId, cancellationToken))
        {
            return;
        }

        var payment = await payments.GetByProviderReferenceAsync(notification.ProviderReference, cancellationToken);
        if (payment is not null)
        {
            Reconcile(payment, notification);
        }

        // Record the event even when no payment matched: the id is consumed, so redelivery is a
        // clean no-op rather than a repeated (fruitless) lookup.
        payments.RecordProcessedWebhook(notification.EventId, notification.EventType);
        await payments.SaveChangesAsync(cancellationToken);
    }

    private void Reconcile(Payment payment, PaymentWebhookNotification notification)
    {
        switch (notification.Kind)
        {
            case PaymentWebhookKind.Captured when payment.TryMarkCaptured(notification.ProviderReference):
                events.Enqueue(new PaymentCaptured(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    payment.TenantId,
                    payment.Id,
                    payment.OrderId,
                    payment.AmountMinor,
                    payment.Currency,
                    payment.ProviderReference!));
                break;

            case PaymentWebhookKind.Failed when payment.TryMarkFailed(notification.FailureReason ?? "payment_failed"):
                events.Enqueue(new PaymentFailed(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    payment.TenantId,
                    payment.Id,
                    payment.OrderId,
                    payment.FailureReason!));
                break;

            case PaymentWebhookKind.Refunded when payment.TryMarkRefunded():
                events.Enqueue(new PaymentRefunded(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    payment.TenantId,
                    payment.Id,
                    payment.OrderId,
                    payment.AmountMinor));
                break;

            default:
                // The transition was already applied (duplicate/out-of-order delivery) — nothing to do.
                break;
        }
    }
}
