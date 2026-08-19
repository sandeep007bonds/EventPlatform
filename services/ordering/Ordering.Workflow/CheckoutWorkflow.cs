namespace Ordering.Workflow;

/// <summary>
/// The durable checkout saga (ADR-0010): validate hold → create order → charge → convert-to-sold →
/// confirm, with compensation (fail order, refund, release hold). The orchestrator is deterministic
/// — it only calls activities and reasons over their results; all I/O lives in the activities, so a
/// crash mid-flight resumes exactly where it left off.
/// </summary>
public sealed class CheckoutWorkflow : Workflow<CheckoutWorkflowInput, CheckoutWorkflowResult>
{
    // How often the saga re-reads the payment from the provider while waiting for the buyer to
    // finish authenticating. This is the *last* of three routes to an outcome — the buyer's browser
    // nudges us the moment it resolves, and Stripe's webhook lands independently — so it only has
    // to cover the case where both are absent (buyer closed the tab AND no webhook reached us).
    // Deliberately unhurried: a tight interval would spend hundreds of provider API calls per
    // abandoned checkout to shave seconds off a case nobody is waiting on.
    private static readonly TimeSpan PaymentPollInterval = TimeSpan.FromSeconds(20);

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

        // 2. Price the order in the event's own currency, not a platform-wide default — the
        //    available payment methods depend on it (Stripe only offers UPI on INR, for instance).
        //    The same call carries the event's tax rate. Falls back to the configured default
        //    currency and no tax if Catalog can't be read, so an unreachable Catalog degrades
        //    rather than failing the checkout outright.
        var pricing = await context.CallActivityAsync<EventPricing>(
            nameof(FetchEventPricingActivity),
            hold.CatalogEventId);

        // 3. Re-check the promo code, if the buyer applied one. The quote they saw was advisory —
        //    a code can expire, be retired, or hit its cap between preview and confirm — so the
        //    charged total is only trustworthy if it is priced from a fresh evaluation.
        //    A rejection fails the checkout rather than silently charging full price: the buyer
        //    agreed to a discounted total, and quietly taking more would be mis-selling.
        var lineSpecs = hold.Lines
            .Select(line => new OrderLineSpec(
                line.InventoryItemId,
                line.SeatId,
                line.GeneralAdmissionAllocationId,
                line.Quantity,
                line.PriceTier,
                line.UnitPriceMinor,
                line.PriceMinor))
            .ToList();

        PromoCodeTerms? promoTerms = null;
        Guid? promoCodeId = null;
        string? promoCodeText = null;

        if (!string.IsNullOrWhiteSpace(input.PromoCode))
        {
            var evaluation = await context.CallActivityAsync<PromoCodeEvaluation>(
                nameof(EvaluatePromoCodeActivity),
                new EvaluatePromoCodeInput(hold.CatalogEventId, input.PromoCode, input.UserId, lineSpecs));

            if (!evaluation.IsAccepted)
            {
                return new CheckoutWorkflowResult(nameof(CheckoutOutcome.PromoCodeInvalid), null);
            }

            promoTerms = evaluation.Terms;
            promoCodeId = evaluation.PromoCodeId;
            promoCodeText = evaluation.Code;
        }

        // 4. Create the order (awaiting payment). Tenant comes from the hold (ADR-0022) — it's the
        //    organizer who owns the event/inventory, not necessarily present on the buyer's own token.
        //    OrderId is pre-minted by the checkout endpoint (it's also this workflow's own instance
        //    id), so a webhook-driven subscriber can raise an event straight back with no lookup.
        var order = await context.CallActivityAsync<CreateOrderOutput>(
            nameof(CreateOrderActivity),
            new CreateOrderInput(
                hold.TenantId,
                input.UserId,
                input.HoldId,
                input.IdempotencyKey,
                hold.CatalogEventId,
                hold.Lines,
                input.BuyerEmail,
                input.OrderId,
                pricing.Currency,
                promoTerms,
                promoCodeId,
                promoCodeText,
                pricing.TaxRatePercent,
                pricing.TaxLabel));

        // A concurrent checkout for the same idempotency key already owns this order — stop here so
        // we never charge twice. The winning saga drives the order to its terminal state; the caller
        // re-fetches (or retries the key) to learn the outcome.
        if (order.AlreadyExisted)
        {
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Duplicate), order.OrderId);
        }

        // 5. Create (not confirm) a payment intent — the buyer authenticates client-side via Stripe's
        //    Payment Element (card 3-D Secure, UPI app-switch, etc.). Record the client secret on the
        //    order so the checkout endpoint's fast-return poll can hand it to the frontend, then
        //    extend the hold to cover however long authentication takes.
        //    A provider outage here would otherwise kill the workflow instance outright, skipping
        //    compensation entirely: the order would sit AwaitingPayment and the seats would stay
        //    held until the reaper collected them — minutes of locked inventory per failed attempt,
        //    at exactly the moment a provider is least likely to be healthy. Catch it and unwind.
        CreateIntentOutput intent;
        try
        {
            intent = await context.CallActivityAsync<CreateIntentOutput>(
                nameof(CreateIntentActivity),
                new CreateIntentInput(hold.TenantId, order.OrderId, order.TotalMinor, order.Currency, input.IdempotencyKey));
        }
        catch (TaskFailedException)
        {
            await context.CallActivityAsync<bool>(
                nameof(FailOrderActivity),
                new FailInput(order.OrderId, "payment_intent_failed"));
            await context.CallActivityAsync<bool>(nameof(ReleaseHoldActivity), input.HoldId);
            return new CheckoutWorkflowResult(nameof(CheckoutOutcome.PaymentFailed), order.OrderId);
        }

        await context.CallActivityAsync<bool>(
            nameof(RecordPaymentIntentActivity),
            new RecordPaymentIntentInput(order.OrderId, intent.ClientSecret));

        var extendedExpiresAt = await context.CallActivityAsync<DateTimeOffset?>(nameof(ExtendHoldActivity), input.HoldId);
        var deadline = extendedExpiresAt?.UtcDateTime ?? context.CurrentUtcDateTime;

        // Wait for the payment to resolve, by whichever route reports it first:
        //
        //   push — Stripe's webhook lands, Ordering's PaymentCaptured/PaymentFailed subscriber
        //          raises "PaymentOutcome" into this instance. Instant, and the production path.
        //   pull — on each tick we ask Payments to re-read the intent straight from Stripe. This is
        //          what makes checkout work where Stripe can't call back (localhost), and a backstop
        //          anywhere a webhook is dropped.
        //
        // Both funnel through the same reconciliation in Payments, and every transition is a
        // TryMark*, so whichever arrives second is a harmless no-op.
        //
        // The external-event subscription is created ONCE, outside the loop, with only the timer
        // recreated per tick — re-subscribing each iteration would leave abandoned waiters that can
        // swallow an event.
        var paymentOutcomeTask = context.WaitForExternalEventAsync<PaymentOutcomeSignal>("PaymentOutcome");

        var captured = false;
        var resolved = false;
        string outcomeOnFailure = nameof(CheckoutOutcome.PaymentTimedOut);
        string failureReason = "payment_timed_out";

        while (!resolved && context.CurrentUtcDateTime < deadline)
        {
            var nextTick = context.CurrentUtcDateTime.Add(PaymentPollInterval);
            if (nextTick > deadline)
            {
                nextTick = deadline;
            }

            using var tickCts = new CancellationTokenSource();
            var tickTask = context.CreateTimer(nextTick, tickCts.Token);
            var winner = await Task.WhenAny(paymentOutcomeTask, tickTask);

            if (winner == paymentOutcomeTask)
            {
                // MUST be the synchronous Cancel(), never CancelAsync(). An orchestrator may only
                // ever await *durable* tasks — awaiting an ordinary Task here hands control to a
                // non-durable continuation, and the executor completes the turn having produced
                // zero actions ("Sending 0 action(s)"), leaving the saga idle forever: the payment
                // is captured, the order never leaves AwaitingPayment, and the buyer's page polls
                // it for eternity. Sonar's S6966 ("await CancelAsync instead") is simply wrong
                // inside an orchestrator, so it is suppressed rather than followed.
#pragma warning disable S6966 // Awaiting CancelAsync() inside an orchestrator breaks replay — see above.
                tickCts.Cancel();
#pragma warning restore S6966
                var signal = await paymentOutcomeTask;
                captured = signal.Captured;
                resolved = true;
                outcomeOnFailure = nameof(CheckoutOutcome.PaymentFailed);
                failureReason = signal.FailureReason ?? "payment_failed";
                break;
            }

            var status = await context.CallActivityAsync<string>(
                nameof(SyncPaymentStatusActivity),
                order.OrderId);

            if (string.Equals(status, "Captured", StringComparison.Ordinal))
            {
                captured = true;
                resolved = true;
            }
            else if (string.Equals(status, "Failed", StringComparison.Ordinal))
            {
                resolved = true;
                outcomeOnFailure = nameof(CheckoutOutcome.PaymentFailed);
                failureReason = "payment_failed";
            }
        }

        if (!captured)
        {
            await context.CallActivityAsync<bool>(
                nameof(FailOrderActivity),
                new FailInput(order.OrderId, failureReason));
            await context.CallActivityAsync<bool>(nameof(ReleaseHoldActivity), input.HoldId);
            return new CheckoutWorkflowResult(outcomeOnFailure, order.OrderId);
        }

        // 6. Convert the hold to a sale. On failure: fail the order, refund, release the hold.
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

        // 7. Confirm.
        var lines = hold.Lines
            .Select(line => new OrderLineSummary(line.SeatId, line.GeneralAdmissionAllocationId, line.Quantity))
            .ToList();
        await context.CallActivityAsync<bool>(
            nameof(ConfirmOrderActivity),
            new ConfirmInput(order.OrderId, hold.TenantId, hold.CatalogEventId, input.UserId, lines));

        return new CheckoutWorkflowResult(nameof(CheckoutOutcome.Confirmed), order.OrderId);
    }
}
