namespace Ordering.Workflow;

/// <summary>Releases a converted hold's sold seats/quantities back to available in Inventory.</summary>
/// <param name="holdClient">The Inventory hold client.</param>
public sealed class ReleaseSoldActivity(IHoldClient holdClient) : WorkflowActivity<CancelSoldInput, bool>
{
    /// <inheritdoc />
    public override Task<bool> RunAsync(WorkflowActivityContext context, CancelSoldInput input) =>
        holdClient.CancelSoldAsync(input.HoldId, input.OrderId, CancellationToken.None);
}
