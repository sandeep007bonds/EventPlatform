namespace Ordering.Workflow;

/// <summary>
/// Extends the hold's expiry once payment authentication begins, so a slow 3-D Secure challenge or
/// UPI app-switch doesn't expire the buyer's seats. Best-effort — its result seeds the saga's wait
/// deadline but is never itself branched on; <c>ConvertActivity</c>'s own expiry check remains the
/// real safety net regardless.
/// </summary>
/// <param name="holdClient">The Inventory hold client.</param>
public sealed class ExtendHoldActivity(IHoldClient holdClient) : WorkflowActivity<Guid, DateTimeOffset?>
{
    /// <inheritdoc />
    public override Task<DateTimeOffset?> RunAsync(WorkflowActivityContext context, Guid holdId) =>
        holdClient.ExtendAsync(holdId, CancellationToken.None);
}
