namespace Ordering.Workflow;

/// <summary>Releases the hold (compensation).</summary>
/// <param name="holdClient">The Inventory hold client.</param>
public sealed class ReleaseHoldActivity(IHoldClient holdClient) : WorkflowActivity<Guid, bool>
{
    /// <inheritdoc />
    public override Task<bool> RunAsync(WorkflowActivityContext context, Guid holdId) =>
        holdClient.ReleaseAsync(holdId, CancellationToken.None);
}
