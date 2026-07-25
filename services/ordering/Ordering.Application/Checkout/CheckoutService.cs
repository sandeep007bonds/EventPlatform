namespace Ordering.Application.Checkout;

/// <summary>
/// Orchestrates the checkout saga: validate hold → create order → charge → convert-to-sold →
/// confirm, with compensation (release hold, refund) on failure. Deduped by idempotency key.
/// </summary>
/// <remarks>
/// This runs the saga in-process and sequentially. The durability upgrade — a Dapr Workflow so the
/// saga survives a crash mid-flight (ADR-0010) — is tracked as follow-up work.
/// </remarks>
/// <param name="orders">The order repository.</param>
/// <param name="holdClient">The Inventory hold client.</param>
/// <param name="paymentClient">The Payment client.</param>
/// <param name="events">The integration-event publisher (outbox).</param>
/// <param name="options">Checkout options.</param>
public sealed class CheckoutService(
    IOrderRepository orders,
    IHoldClient holdClient,
    IPaymentClient paymentClient,
    IEventPublisher events,
    CheckoutOptions options)
{
    /// <summary>Runs checkout for a hold, idempotently.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="holdId">The hold to purchase.</param>
    /// <param name="idempotencyKey">Idempotency key (unique per tenant).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The checkout result.</returns>
    public async Task<CheckoutResult> CheckoutAsync(
        Guid tenantId,
        Guid userId,
        Guid holdId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        // Idempotency: a prior attempt with this key wins.
        var existing = await orders.GetByIdempotencyKeyAsync(tenantId, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.Status == OrderStatus.Confirmed
                ? CheckoutResult.Confirmed(existing.Id)
                : CheckoutResult.Failed(CheckoutOutcome.Failed, existing.Id);
        }

        // Validate the hold (owner, active, not expired).
        var hold = await holdClient.GetHoldAsync(holdId, cancellationToken);
        if (hold is null)
        {
            return CheckoutResult.Failed(CheckoutOutcome.HoldNotFound);
        }

        if (hold.UserId != userId)
        {
            return CheckoutResult.Failed(CheckoutOutcome.Forbidden);
        }

        if (!string.Equals(hold.Status, "Active", StringComparison.Ordinal))
        {
            return CheckoutResult.Failed(CheckoutOutcome.HoldNotActive);
        }

        if (hold.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return CheckoutResult.Failed(CheckoutOutcome.HoldExpired);
        }

        // Create the order (awaiting payment). The unique (tenant, idempotency_key) index is the
        // backstop against a concurrent duplicate.
        var order = Order.Create(
            tenantId,
            userId,
            hold.CatalogEventId,
            holdId,
            options.DefaultCurrency,
            idempotencyKey,
            hold.Lines.Select(line => new OrderLineSpec(line.InventoryItemId, line.SeatId, line.PriceMinor)));
        order.MarkAwaitingPayment();
        orders.Add(order);
        await orders.SaveChangesAsync(cancellationToken);

        // Charge.
        var payment = await paymentClient.ChargeAsync(
            order.Id,
            order.TotalMinor,
            order.Currency,
            idempotencyKey,
            cancellationToken);
        if (!payment.Succeeded)
        {
            order.MarkFailed(payment.FailureReason ?? "payment_failed");
            await orders.SaveChangesAsync(cancellationToken);
            await holdClient.ReleaseAsync(holdId, cancellationToken);
            return CheckoutResult.Failed(CheckoutOutcome.PaymentFailed, order.Id);
        }

        // Convert the hold to a sale.
        if (!await holdClient.ConvertAsync(holdId, order.Id, cancellationToken))
        {
            order.MarkFailed("convert_failed");
            await orders.SaveChangesAsync(cancellationToken);
            await paymentClient.RefundAsync(order.Id, idempotencyKey, cancellationToken);
            await holdClient.ReleaseAsync(holdId, cancellationToken);
            return CheckoutResult.Failed(CheckoutOutcome.ConvertFailed, order.Id);
        }

        // Confirm.
        order.MarkConfirmed();
        events.Enqueue(new OrderConfirmed(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            tenantId,
            order.Id,
            hold.CatalogEventId,
            userId,
            order.TotalMinor,
            order.Currency,
            hold.Lines.Select(line => line.SeatId).ToList()));
        await orders.SaveChangesAsync(cancellationToken);

        return CheckoutResult.Confirmed(order.Id);
    }
}
