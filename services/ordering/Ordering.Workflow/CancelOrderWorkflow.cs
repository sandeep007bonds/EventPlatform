namespace Ordering.Workflow;

/// <summary>
/// The durable order-cancellation saga, a buyer-initiated counterpart to <see cref="CheckoutWorkflow"/>:
/// void tickets → release sold inventory → refund payment → mark the order refunded. Void-then-
/// release-then-refund, deliberately the opposite order to <see cref="CheckoutWorkflow"/>'s own
/// compensation (which refunds before releasing the hold) — that path is unwinding a checkout that
/// never actually completed, while this one unwinds a fully completed sale, so the product (seats/
/// tickets) is taken back before the money is returned. Each activity is individually idempotent
/// (gated on the underlying aggregate's own status), so a crash mid-flight resumes safely on replay.
/// </summary>
public sealed class CancelOrderWorkflow : Workflow<CancelOrderWorkflowInput, CancelOrderWorkflowResult>
{
    /// <inheritdoc />
    public override async Task<CancelOrderWorkflowResult> RunAsync(WorkflowContext context, CancelOrderWorkflowInput input)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(input);

        var order = await context.CallActivityAsync<OrderSnapshot?>(nameof(FetchOrderActivity), input.OrderId);
        if (order is null)
        {
            return new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.OrderNotFound), null);
        }

        if (order.UserId != input.UserId)
        {
            return new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.Forbidden), order.OrderId);
        }

        // Idempotent: a retried/replayed cancellation of an already-refunded order is a success, not
        // an error — the buyer's request has already been honored.
        if (string.Equals(order.Status, nameof(OrderStatus.Refunded), StringComparison.Ordinal))
        {
            return new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.Cancelled), order.OrderId);
        }

        if (!string.Equals(order.Status, nameof(OrderStatus.Confirmed), StringComparison.Ordinal))
        {
            return new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.NotConfirmed), order.OrderId);
        }

        // 1. Void every ticket first — a buyer who already used one (checked in at the gate) cannot
        //    cancel the order out from under it.
        var voidResult = await context.CallActivityAsync<VoidTicketsClientResult>(nameof(VoidTicketsActivity), order.OrderId);
        if (!voidResult.Succeeded)
        {
            return voidResult.AlreadyCheckedIn
                ? new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.TicketAlreadyCheckedIn), order.OrderId)
                : new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.Failed), order.OrderId);
        }

        // 2. Release the sold seats/quantities back to available.
        var released = await context.CallActivityAsync<bool>(
            nameof(ReleaseSoldActivity),
            new CancelSoldInput(order.HoldId, order.OrderId));
        if (!released)
        {
            return new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.Failed), order.OrderId);
        }

        // 3. Refund the payment (best-effort — a failed refund is retried by ops, not the saga; see
        //    RefundActivity).
        await context.CallActivityAsync<bool>(
            nameof(RefundActivity),
            new RefundInput(order.OrderId, order.IdempotencyKey));

        // 4. Mark the order refunded.
        await context.CallActivityAsync<bool>(nameof(MarkOrderRefundedActivity), order.OrderId);

        return new CancelOrderWorkflowResult(nameof(CancelOrderOutcome.Cancelled), order.OrderId);
    }
}
