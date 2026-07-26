namespace Ordering.Workflow;

/// <summary>Converts the hold to a sale in Inventory.</summary>
/// <param name="holdClient">The Inventory hold client.</param>
public sealed class ConvertActivity(IHoldClient holdClient) : WorkflowActivity<ConvertInput, bool>
{
    /// <inheritdoc />
    public override Task<bool> RunAsync(WorkflowActivityContext context, ConvertInput input) =>
        holdClient.ConvertAsync(input.HoldId, input.OrderId, CancellationToken.None);
}
