namespace Ordering.Workflow;

/// <summary>
/// Asks Payments to re-read the order's payment from the provider. The saga calls this on a timer
/// while waiting for authentication to finish, so it can resolve a payment by asking rather than
/// only by being told via webhook (ADR-0028).
/// </summary>
/// <param name="payments">The Payment client.</param>
public sealed class SyncPaymentStatusActivity(IPaymentClient payments) : WorkflowActivity<Guid, string>
{
    /// <inheritdoc />
    public override Task<string> RunAsync(WorkflowActivityContext context, Guid orderId) =>
        payments.SyncStatusAsync(orderId, CancellationToken.None);
}
