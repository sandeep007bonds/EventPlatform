namespace Ordering.Workflow;

/// <summary>
/// Records the created intent's client secret on the order, so the checkout endpoint's fast-return
/// poll (and a buyer reload/redirect-return mid-authentication) can read it back.
/// </summary>
/// <param name="orders">The order repository.</param>
public sealed class RecordPaymentIntentActivity(IOrderRepository orders) : WorkflowActivity<RecordPaymentIntentInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, RecordPaymentIntentInput input)
    {
        var order = await orders.GetByIdAsync(input.OrderId, CancellationToken.None);
        if (order is null)
        {
            return false;
        }

        order.RecordPaymentClientSecret(input.ClientSecret);
        await orders.SaveChangesAsync(CancellationToken.None);
        return true;
    }
}
