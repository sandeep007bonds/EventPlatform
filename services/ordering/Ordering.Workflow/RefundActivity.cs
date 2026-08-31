namespace Ordering.Workflow;

/// <summary>Refunds the payment (compensation).</summary>
/// <param name="payments">The Payment client.</param>
public sealed class RefundActivity(IPaymentClient payments) : WorkflowActivity<RefundInput, bool>
{
    /// <inheritdoc />
    public override async Task<bool> RunAsync(WorkflowActivityContext context, RefundInput input)
    {
        await payments.RefundAsync(input.OrderId, input.IdempotencyKey, input.AmountMinor, CancellationToken.None);
        return true;
    }
}
