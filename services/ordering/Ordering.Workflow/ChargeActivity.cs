namespace Ordering.Workflow;

/// <summary>Charges the order through the Payment service.</summary>
/// <param name="payments">The Payment client.</param>
public sealed class ChargeActivity(IPaymentClient payments) : WorkflowActivity<ChargeInput, ChargeOutput>
{
    /// <inheritdoc />
    public override async Task<ChargeOutput> RunAsync(WorkflowActivityContext context, ChargeInput input)
    {
        var result = await payments.ChargeAsync(
            input.TenantId,
            input.OrderId,
            input.AmountMinor,
            input.Currency,
            input.IdempotencyKey,
            input.PaymentMethodId,
            CancellationToken.None);

        return new ChargeOutput(result.Succeeded, result.FailureReason);
    }
}
