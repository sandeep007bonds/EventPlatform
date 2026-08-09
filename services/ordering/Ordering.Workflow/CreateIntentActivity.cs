namespace Ordering.Workflow;

/// <summary>Creates a payment intent for the order through the Payment service (does not confirm it).</summary>
/// <param name="payments">The Payment client.</param>
public sealed class CreateIntentActivity(IPaymentClient payments) : WorkflowActivity<CreateIntentInput, CreateIntentOutput>
{
    /// <inheritdoc />
    public override async Task<CreateIntentOutput> RunAsync(WorkflowActivityContext context, CreateIntentInput input)
    {
        var result = await payments.CreateIntentAsync(
            input.TenantId,
            input.OrderId,
            input.AmountMinor,
            input.Currency,
            input.IdempotencyKey,
            CancellationToken.None);

        return new CreateIntentOutput(result.ProviderReference, result.ClientSecret);
    }
}
