namespace Ordering.Workflow;

/// <summary>Reads a hold from Inventory — the I/O the workflow orchestrator cannot do directly.</summary>
/// <param name="holdClient">The Inventory hold client.</param>
public sealed class FetchHoldActivity(IHoldClient holdClient) : WorkflowActivity<Guid, HoldSnapshot?>
{
    /// <inheritdoc />
    public override Task<HoldSnapshot?> RunAsync(WorkflowActivityContext context, Guid holdId) =>
        holdClient.GetHoldAsync(holdId, CancellationToken.None);
}
