namespace Ordering.Workflow;

/// <summary>Marks the order refunded, the terminal step of a successful cancellation.</summary>
/// <param name="orders">The order repository.</param>
public sealed class MarkOrderRefundedActivity(IOrderRepository orders) : WorkflowActivity<Guid, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, Guid orderId)
    {
        var order = await orders.GetByIdAsync(orderId, CancellationToken.None);
        if (order is null || order.Status != OrderStatus.Confirmed)
        {
            // Already refunded by a prior run of this activity (replay) — idempotent no-op.
            return true;
        }

        order.MarkRefunded();
        await orders.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
