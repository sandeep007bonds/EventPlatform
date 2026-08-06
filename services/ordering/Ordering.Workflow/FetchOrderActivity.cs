namespace Ordering.Workflow;

/// <summary>Reads an order from Ordering's own database, for the cancellation saga.</summary>
/// <param name="orders">The order repository.</param>
public sealed class FetchOrderActivity(IOrderRepository orders) : WorkflowActivity<Guid, OrderSnapshot?>
{
    /// <inheritdoc />
    public override async Task<OrderSnapshot?> RunAsync(WorkflowActivityContext context, Guid orderId)
    {
        var order = await orders.GetByIdAsync(orderId, CancellationToken.None);
        return order is null
            ? null
            : new OrderSnapshot(order.Id, order.UserId, order.HoldId, order.Status.ToString(), order.IdempotencyKey);
    }
}
