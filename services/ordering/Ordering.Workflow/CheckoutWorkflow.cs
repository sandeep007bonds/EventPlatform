namespace Ordering.Workflow;

/// <summary>
/// The durable checkout saga (ADR-0010): validate hold → create order → charge → convert-to-sold →
/// confirm, with compensation (fail order, refund, release hold). The orchestrator is deterministic
/// — it only calls activities and reasons over their results; all I/O lives in the activities, so a
/// crash mid-flight resumes exactly where it left off.
/// </summary>
public sealed class CheckoutWorkflow : Workflow<CheckoutWorkflowInput, CheckoutWorkflowResult>
{
    /// <inheritdoc />
    public override async Task<CheckoutWorkflowResult> RunAsync(WorkflowContext context, CheckoutWorkflowInput input)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        // 1. Validate the hold (owner, active, not expired). The checks are deterministic; the read
        //    is an activity.
        var hold = await context.CallActivityAsync<HoldSnapshot?>(nameof(FetchHoldActivity), input.HoldId);
        if (hold is null)
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.HoldNotFound), null);
        }

        if (hold.UserId != input.UserId)
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Forbidden), null);
        }

        if (!string.Equals(hold.Status, "Active", StringComparison.Ordinal))
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.HoldNotActive), null);
        }

        if (hold.ExpiresAt.UtcDateTime < context.CurrentUtcDateTime)
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.HoldExpired), null);
        }

        // 2. Create the order (awaiting payment).
        var order = await context.CallActivityAsync<CreateOrderOutput>(
            nameof(CreateOrderActivity),
            new CreateOrderInput(input.TenantId, input.UserId, input.HoldId, input.IdempotencyKey, hold.CatalogEventId, hold.Lines, input.BuyerEmail));

        // A concurrent checkout for the same idempotency key already owns this order — stop here so
        // we never charge twice. The winning saga drives the order to its terminal state; the caller
        // re-fetches (or retries the key) to learn the outcome.
        if (order.AlreadyExisted)
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Duplicate), order.OrderId);
        }

        // 3. Charge. On failure: fail the order and release the hold.
        var charge = await context.CallActivityAsync<ChargeOutput>(
            nameof(ChargeActivity),
            new ChargeInput(input.TenantId, order.OrderId, order.TotalMinor, order.Currency, input.IdempotencyKey));
        if (!charge.Succeeded)
        {
            await context.CallActivityAsync<bool>(
                nameof(FailOrderActivity),
                new FailInput(order.OrderId, charge.FailureReason ?? "payment_failed"));
            await context.CallActivityAsync<bool>(nameof(ReleaseHoldActivity), input.HoldId);
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.PaymentFailed), order.OrderId);
        }

        // 4. Convert the hold to a sale. On failure: fail the order, refund, release the hold.
        var converted = await context.CallActivityAsync<bool>(
            nameof(ConvertActivity),
            new ConvertInput(input.HoldId, order.OrderId));
        if (!converted)
        {
            await context.CallActivityAsync<bool>(nameof(FailOrderActivity), new FailInput(order.OrderId, "convert_failed"));
            await context.CallActivityAsync<bool>(nameof(RefundActivity), new RefundInput(order.OrderId, input.IdempotencyKey));
            await context.CallActivityAsync<bool>(nameof(ReleaseHoldActivity), input.HoldId);
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.ConvertFailed), order.OrderId);
        }

        // 5. Confirm.
        var lines = hold.Lines
            .Select(line => new OrderLineSummary(line.SeatId, line.GeneralAdmissionAllocationId, line.Quantity))
            .ToList();
        await context.CallActivityAsync<bool>(
            nameof(ConfirmOrderActivity),
            new ConfirmInput(order.OrderId, input.TenantId, hold.CatalogEventId, input.UserId, lines));

        return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Confirmed), order.OrderId);
    }
}
