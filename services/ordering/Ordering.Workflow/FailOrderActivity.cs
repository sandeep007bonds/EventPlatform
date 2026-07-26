namespace Ordering.Workflow;

/// <summary>Marks the order failed (compensation).</summary>
/// <param name="orders">The order repository.</param>
public sealed class FailOrderActivity(IOrderRepository orders) : WorkflowActivity<FailInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, FailInput input)
    {
        var order = await orders.GetByIdAsync(input.OrderId, CancellationToken.None);
        if (order is null)
        {
            return false;
        }

        order.MarkFailed(input.Reason);
        await orders.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
