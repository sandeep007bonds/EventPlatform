namespace Ordering.Workflow;

/// <summary>Creates the order (awaiting payment) and persists it.</summary>
/// <param name="orders">The order repository.</param>
public sealed class CreateOrderActivity(IOrderRepository orders)
    : WorkflowActivity<CreateOrderInput, CreateOrderOutput>
{
    /// <inheritdoc />
    public override async Task<CreateOrderOutput> RunAsync(WorkflowActivityContext context, CreateOrderInput input)
    {
        // Fast path: a prior attempt for this key already created the order (activities may be
        // retried, and a concurrent request may have won). Reuse it rather than inserting again.
        var existing = await orders.GetByIdempotencyKeyAsync(input.UserId, input.IdempotencyKey, CancellationToken.None);
        if (existing is not null)
        {
            return new CreateOrderOutput(existing.Id, existing.TotalMinor, existing.Currency, AlreadyExisted: true);
        }

        var order = Order.Create(
            input.OrderId,
            input.TenantId,
            input.UserId,
            input.CatalogEventId,
            input.HoldId,
            input.Currency,
            input.IdempotencyKey,
            input.Lines.Select(line => new OrderLineSpec(
                line.InventoryItemId,
                line.SeatId,
                line.GeneralAdmissionAllocationId,
                line.Quantity,
                line.PriceTier,
                line.UnitPriceMinor,
                line.PriceMinor)),
            input.BuyerEmail,
            input.PromoTerms,
            input.PromoCodeId,
            input.PromoCodeText,
            input.TaxRatePercent,
            input.TaxLabel,
            input.BookingFeePerTicketMinor);
        order.MarkAwaitingPayment();

        // Race window: two attempts both passed the check above. The unique index on
        // (user_id, idempotency_key) lets exactly one insert win; the loser re-fetches the winner.
        if (await orders.TryAddAsync(order, CancellationToken.None))
        {
            return new CreateOrderOutput(order.Id, order.TotalMinor, order.Currency, AlreadyExisted: false);
        }

        var winner = await orders.GetByIdempotencyKeyAsync(input.UserId, input.IdempotencyKey, CancellationToken.None)
            ?? throw new InvalidOperationException("Duplicate order insert was rejected but no existing order was found.");
        return new CreateOrderOutput(winner.Id, winner.TotalMinor, winner.Currency, AlreadyExisted: true);
    }
}
