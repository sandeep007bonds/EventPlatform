namespace Ordering.Workflow;

/// <summary>Creates the order (awaiting payment) and persists it.</summary>
/// <param name="orders">The order repository.</param>
/// <param name="options">Checkout options (currency).</param>
public sealed class CreateOrderActivity(IOrderRepository orders, CheckoutOptions options)
    : WorkflowActivity<CreateOrderInput, CreateOrderOutput>
{
    /// <inheritdoc />
    public override async Task<CreateOrderOutput> RunAsync(WorkflowActivityContext context, CreateOrderInput input)
    {
        var order = Order.Create(
            input.TenantId,
            input.UserId,
            input.CatalogEventId,
            input.HoldId,
            options.DefaultCurrency,
            input.IdempotencyKey,
            input.Lines.Select(line => new OrderLineSpec(line.InventoryItemId, line.SeatId, line.PriceMinor)));
        order.MarkAwaitingPayment();

        orders.Add(order);
        await orders.SaveChangesAsync(CancellationToken.None);

        return new CreateOrderOutput(order.Id, order.TotalMinor, order.Currency);
    }
}
