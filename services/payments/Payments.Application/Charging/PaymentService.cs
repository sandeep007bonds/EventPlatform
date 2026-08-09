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
    /// <summary>Creates (or re-fetches an idempotent) payment intent for an order.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The order being paid.</param>
    /// <param name="amountMinor">Amount in minor units.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="idempotencyKey">Idempotency key (unique per order).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The intent result.</returns>
    public async Task<CreateIntentResult> CreatePaymentIntentAsync(
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
            // A retried/duplicate call must hand back the same client secret rather than re-calling
            // the gateway — this is exactly why ClientSecret is persisted, not just returned transiently.
            return ToIntentResult(existing);
        }

        var payment = Payment.Create(tenantId, orderId, gateway.Provider, idempotencyKey, amountMinor, currency);
        payments.Add(payment);

        var intent = await gateway.CreateIntentAsync(amountMinor, currency, idempotencyKey, cancellationToken);
        payment.RecordIntentDetails(intent.Reference, intent.ClientSecret);

        if (intent.CapturedImmediately)
        {
            payment.MarkCaptured(intent.Reference);
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

        // Real Stripe path: the payment stays Initiated. No PaymentCaptured/PaymentFailed here — the
        // outcome arrives later exclusively via the webhook (StripeWebhookGateway/PaymentWebhookService).

        // Race window: two creates for the same (order, key) both passed the pre-check. The unique
        // index lets exactly one persist; the loser re-fetches the winner (the gateway is idempotent
        // on the key, so no duplicate intent, and the loser's outbox events roll back with the save).
        if (await payments.TrySaveChangesAsync(cancellationToken))
        {
            return ToIntentResult(payment);
        }

        var winner = await payments.GetByOrderAndKeyAsync(orderId, idempotencyKey, cancellationToken)
            ?? throw new InvalidOperationException("Duplicate intent create was rejected but no existing payment was found.");
        return ToIntentResult(winner);
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

    private static CreateIntentResult ToIntentResult(Payment payment) =>
        new(
            payment.Id,
            payment.ProviderReference ?? throw new InvalidOperationException($"Payment {payment.Id} has no provider reference."),
            payment.ClientSecret ?? throw new InvalidOperationException($"Payment {payment.Id} has no client secret."),
            payment.Status == PaymentStatus.Captured);
}
