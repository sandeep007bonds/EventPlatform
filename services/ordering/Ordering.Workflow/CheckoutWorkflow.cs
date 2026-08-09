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

        // 2. Create the order (awaiting payment). Tenant comes from the hold (ADR-0022) — it's the
        //    organizer who owns the event/inventory, not necessarily present on the buyer's own token.
        //    OrderId is pre-minted by the checkout endpoint (it's also this workflow's own instance
        //    id), so a webhook-driven subscriber can raise an event straight back with no lookup.
        var order = await context.CallActivityAsync<CreateOrderOutput>(
            nameof(CreateOrderActivity),
            new CreateOrderInput(
                hold.TenantId, input.UserId, input.HoldId, input.IdempotencyKey, hold.CatalogEventId, hold.Lines, input.BuyerEmail, input.OrderId));

        // A concurrent checkout for the same idempotency key already owns this order — stop here so
        // we never charge twice. The winning saga drives the order to its terminal state; the caller
        // re-fetches (or retries the key) to learn the outcome.
        if (order.AlreadyExisted)
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Duplicate), order.OrderId);
        }

        // 3. Create (not confirm) a payment intent — the buyer authenticates client-side via Stripe's
        //    Payment Element (card 3-D Secure, UPI app-switch, etc.). Record the client secret on the
        //    order so the checkout endpoint's fast-return poll can hand it to the frontend, then
        //    extend the hold to cover however long authentication takes.
        var intent = await context.CallActivityAsync<CreateIntentOutput>(
            nameof(CreateIntentActivity),
            new CreateIntentInput(hold.TenantId, order.OrderId, order.TotalMinor, order.Currency, input.IdempotencyKey));

        await context.CallActivityAsync<bool>(
            nameof(RecordPaymentIntentActivity),
            new RecordPaymentIntentInput(order.OrderId, intent.ClientSecret));

        var extendedExpiresAt = await context.CallActivityAsync<DateTimeOffset?>(nameof(ExtendHoldActivity), input.HoldId);
        var deadline = extendedExpiresAt?.UtcDateTime ?? context.CurrentUtcDateTime;

        // Race the async payment outcome (raised by Ordering's PaymentCaptured/PaymentFailed webhook
        // subscriber) against the extended-hold deadline. Standard Dapr Workflow/Durable Task
        // external-event-with-timeout idiom — first use in this repo, so double-check the exact
        // CreateTimer/WaitForExternalEventAsync overloads against the installed Dapr.Workflow version
        // during a real build.
        using var timeoutCts = new CancellationTokenSource();
        var paymentOutcomeTask = context.WaitForExternalEventAsync<PaymentOutcomeSignal>("PaymentOutcome");
        var timeoutTask = context.CreateTimer(deadline, timeoutCts.Token);
        var winner = await Task.WhenAny(paymentOutcomeTask, timeoutTask);

        var captured = false;
        string outcomeOnFailure = nameof(CheckoutOutcome.PaymentTimedOut);
        string failureReason = "payment_timed_out";
        if (winner == paymentOutcomeTask)
        {
            timeoutCts.Cancel();
            var signal = await paymentOutcomeTask;
            captured = signal.Captured;
            outcomeOnFailure = nameof(CheckoutOutcome.PaymentFailed);
            failureReason = signal.FailureReason ?? "payment_failed";
        }

        if (!captured)
        {
            await context.CallActivityAsync<bool>(
                nameof(FailOrderActivity),
                new FailInput(order.OrderId, failureReason));
            await context.CallActivityAsync<bool>(nameof(ReleaseHoldActivity), input.HoldId);
            return new CheckoutWorkflowResult(outcomeOnFailure, order.OrderId);
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
            new ConfirmInput(order.OrderId, hold.TenantId, hold.CatalogEventId, input.UserId, lines));

        return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Confirmed), order.OrderId);
    }
}
