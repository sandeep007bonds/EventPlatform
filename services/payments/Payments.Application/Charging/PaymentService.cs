namespace Payments.Application.Charging;

/// <summary>
/// Charges and refunds payments through the configured gateway, idempotently per
/// <c>(order, idempotency key)</c>, and emits payment events via the outbox.
/// </summary>
/// <param name="payments">The payment repository.</param>
/// <param name="gateway">The payment gateway.</param>
/// <param name="events">The integration-event publisher (outbox).</param>
public sealed class PaymentService(
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IEventPublisher events)
{
    /// <summary>Charges an order, idempotently.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The order being paid.</param>
    /// <param name="amountMinor">Amount in minor units.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key (unique per order).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The charge result.</returns>
    public async Task<ChargeResult> ChargeAsync(
        Guid tenantId,
        Guid orderId,
        long amountMinor,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = await payments.GetByOrderAndKeyAsync(orderId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.Status == PaymentStatus.Captured
                ? new ChargeResult(ChargeOutcome.Captured, existing.Id, existing.ProviderReference, null)
                : new ChargeResult(ChargeOutcome.Failed, existing.Id, null, existing.FailureReason ?? "payment_failed");
        }

        var payment = Payment.Create(tenantId, orderId, gateway.Provider, idempotencyKey, amountMinor, currency);
        payments.Add(payment);

        var result = await gateway.ChargeAsync(amountMinor, currency, idempotencyKey, cancellationToken);
        if (result.Succeeded)
        {
            payment.MarkCaptured(result.Reference ?? "unknown");
            events.Enqueue(new PaymentCaptured(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                tenantId,
                payment.Id,
                orderId,
                amountMinor,
                currency,
                payment.ProviderReference!));
        }
        else
        {
            // Keep the provider reference on a non-captured payment (e.g. requires_action / 3-D
            // Secure) so an out-of-band webhook can later reconcile it to a capture.
            if (result.Reference is not null)
            {
                payment.RecordProviderReference(result.Reference);
            }

            payment.MarkFailed(result.FailureReason ?? "payment_failed");
            events.Enqueue(new PaymentFailed(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                tenantId,
                payment.Id,
                orderId,
                payment.FailureReason!));
        }

        await payments.SaveChangesAsync(cancellationToken);

        return payment.Status == PaymentStatus.Captured
            ? new ChargeResult(ChargeOutcome.Captured, payment.Id, payment.ProviderReference, null)
            : new ChargeResult(ChargeOutcome.Failed, payment.Id, null, payment.FailureReason);
    }

    /// <summary>Refunds the captured payment for an order, if any. Idempotent.</summary>
    /// <param name="orderId">The order to refund.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if a payment was refunded.</returns>
    public async Task<bool> RefundAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await payments.GetCapturedByOrderAsync(orderId, cancellationToken);
        if (payment is null)
        {
            return false;
        }

        payment.MarkRefunded();
        if (payment.ProviderReference is not null)
        {
            await gateway.RefundAsync(payment.ProviderReference, cancellationToken);
        }

        events.Enqueue(new PaymentRefunded(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            payment.TenantId,
            payment.Id,
            orderId,
            payment.AmountMinor));

        await payments.SaveChangesAsync(cancellationToken);
        return true;
    }
}
