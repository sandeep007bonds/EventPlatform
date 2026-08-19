namespace Ordering.Tests.Checkout;

/// <summary>
/// Decision-logic tests for <see cref="CheckoutWorkflow"/>, driven through a substituted
/// <see cref="WorkflowContext"/>. They assert *which activities the saga schedules* for a given set
/// of activity results — the compensation ordering, the terminal outcome, and (most importantly)
/// that a captured payment actually reaches convert-and-confirm rather than quietly stalling.
/// </summary>
public sealed class CheckoutWorkflowTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HoldId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EventId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReturnsHoldNotFound_WhenTheHoldDoesNotExist()
    {
        var context = CreateContext();
        StubHold(context, hold: null);

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.HoldNotFound));
        result.OrderId.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnsForbidden_WhenTheHoldBelongsToAnotherBuyer()
    {
        var context = CreateContext();
        StubHold(context, CreateHold(userId: Guid.NewGuid()));

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.Forbidden));
    }

    [Fact]
    public async Task ReturnsHoldNotActive_WhenTheHoldWasAlreadyConverted()
    {
        var context = CreateContext();
        StubHold(context, CreateHold(status: "Converted"));

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.HoldNotActive));
    }

    [Fact]
    public async Task ReturnsHoldExpired_WhenTheHoldLapsedBeforeCheckout()
    {
        var context = CreateContext();
        StubHold(context, CreateHold(expiresAt: Now.AddMinutes(-1)));

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.HoldExpired));
    }

    [Fact]
    public async Task ReturnsDuplicate_WhenAConcurrentCheckoutAlreadyOwnsTheOrder()
    {
        var context = CreateContext();
        StubHold(context, CreateHold());
        StubCurrency(context);
        StubCreateOrder(context, alreadyExisted: true);

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.Duplicate));
        result.OrderId.ShouldBe(OrderId);

        // A duplicate must never reach the money: no intent is created for the losing request.
        await context.DidNotReceive().CallActivityAsync<CreateIntentOutput>(
            nameof(CreateIntentActivity), Arg.Any<object?>());
    }

    // The regression test for the stall that shipped in ADR-0028's first cut: the saga received its
    // PaymentOutcome event and then scheduled nothing at all, leaving a captured payment with an
    // order stuck in AwaitingPayment forever. Asserting that convert *and* confirm are both
    // scheduled is what makes that failure visible here rather than at a real card reader.
    [Fact]
    public async Task ConfirmsTheOrder_WhenThePaymentOutcomeEventReportsCapture()
    {
        var context = CreateHappyPathContext();
        StubPaymentOutcome(context, new PaymentOutcomeSignal(Captured: true, FailureReason: null));
        StubConvert(context, converted: true);

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.Confirmed));
        result.OrderId.ShouldBe(OrderId);

        await context.Received().CallActivityAsync<bool>(nameof(ConvertActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ConfirmOrderActivity), Arg.Any<object?>());
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>());
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>());
    }

    // The pull half of the same guarantee: no event ever arrives, but the saga's own poll of
    // Payments reports a capture. It must reach the same terminal state — this is what keeps
    // checkout working where the provider cannot call back (localhost, or a dropped webhook).
    [Fact]
    public async Task ConfirmsTheOrder_WhenOnlyThePollReportsCapture()
    {
        var context = CreateHappyPathContext();
        StubNoPaymentOutcome(context);
        StubTimerFires(context);
        StubSyncStatus(context, "Captured");
        StubConvert(context, converted: true);

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.Confirmed));
        await context.Received().CallActivityAsync<string>(nameof(SyncPaymentStatusActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ConvertActivity), Arg.Any<object?>());
    }

    [Fact]
    public async Task CompensatesAndFails_WhenThePaymentOutcomeEventReportsFailure()
    {
        var context = CreateHappyPathContext();
        StubPaymentOutcome(context, new PaymentOutcomeSignal(Captured: false, FailureReason: "card_declined"));

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.PaymentFailed));

        await context.Received().CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>());

        // Nothing was captured, so nothing may be refunded — a refund here would be a real defect.
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(RefundActivity), Arg.Any<object?>());
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(ConvertActivity), Arg.Any<object?>());
    }

    [Fact]
    public async Task CompensatesAndFails_WhenThePollReportsFailure()
    {
        var context = CreateHappyPathContext();
        StubNoPaymentOutcome(context);
        StubTimerFires(context);
        StubSyncStatus(context, "Failed");

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.PaymentFailed));
        await context.Received().CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>());
    }

    // Money already moved by the time convert-to-sold failed, so the refund is mandatory — this
    // asserts the compensation that stops a buyer paying for seats they never received.
    [Fact]
    public async Task RefundsAndReleases_WhenConvertToSoldFails()
    {
        var context = CreateHappyPathContext();
        StubPaymentOutcome(context, new PaymentOutcomeSignal(Captured: true, FailureReason: null));
        StubConvert(context, converted: false);

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.ConvertFailed));

        await context.Received().CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(RefundActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>());
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(ConfirmOrderActivity), Arg.Any<object?>());
    }

    // A payment-provider outage. Left uncaught this kills the workflow instance outright, skipping
    // compensation: the order sits AwaitingPayment and the seats stay held until the reaper takes
    // them — minutes of locked inventory per attempt, exactly when the provider is least healthy.
    [Fact]
    public async Task CompensatesAndFails_WhenCreatingThePaymentIntentThrows()
    {
        var context = CreateContext();
        StubHold(context, CreateHold());
        StubCurrency(context);
        StubCreateOrder(context, alreadyExisted: false);
        context.CallActivityAsync<CreateIntentOutput>(nameof(CreateIntentActivity), Arg.Any<object?>())
            .Throws(new TaskFailedException(
                nameof(CreateIntentActivity),
                taskId: 1,
                new TaskFailureDetails("StripeException", "provider unavailable", null, null)));

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.PaymentFailed));
        result.OrderId.ShouldBe(OrderId);

        await context.Received().CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>());

        // No intent was ever created, so there is nothing to refund and nothing to convert.
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(RefundActivity), Arg.Any<object?>());
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(ConvertActivity), Arg.Any<object?>());
    }

    // The buyer abandoned authentication: the extended hold deadline has already passed, so the
    // saga never enters the wait loop and compensates straight away, freeing the seats.
    [Fact]
    public async Task TimesOut_WhenTheExtendedHoldDeadlineHasAlreadyPassed()
    {
        var context = CreateHappyPathContext(extendedExpiresAt: Now.AddMinutes(-1));
        StubNoPaymentOutcome(context);

        var result = await new CheckoutWorkflow().RunAsync(context, CreateInput());

        result.Outcome.ShouldBe(nameof(CheckoutOutcome.PaymentTimedOut));
        await context.Received().CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>());
        await context.Received().CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>());
        await context.DidNotReceive().CallActivityAsync<bool>(nameof(RefundActivity), Arg.Any<object?>());
    }

    private static CheckoutWorkflowInput CreateInput() =>
        new(UserId, HoldId, "idem-key-1", "buyer@example.com", OrderId);

    private static HoldSnapshot CreateHold(
        Guid? userId = null,
        string status = "Active",
        DateTimeOffset? expiresAt = null) =>
        new(
            HoldId,
            TenantId,
            EventId,
            userId ?? UserId,
            status,
            expiresAt ?? Now.AddMinutes(2),
            1000,
            [new HoldLineSnapshot(Guid.NewGuid(), Guid.NewGuid(), null, 1, "A", 1000, 1000)]);

    private static WorkflowContext CreateContext()
    {
        var context = Substitute.For<WorkflowContext>();
        context.CurrentUtcDateTime.Returns(Now);
        return context;
    }

    // Stubs every step up to (and including) the hold extension, leaving the wait open.
    private static WorkflowContext CreateHappyPathContext(DateTimeOffset? extendedExpiresAt = null)
    {
        var context = CreateContext();
        StubHold(context, CreateHold());
        StubCurrency(context);
        StubCreateOrder(context, alreadyExisted: false);
        StubCreateIntent(context);
        StubExtendHold(context, extendedExpiresAt ?? Now.AddMinutes(15));
        return context;
    }

    private static void StubHold(WorkflowContext context, HoldSnapshot? hold) =>
        context.CallActivityAsync<HoldSnapshot?>(nameof(FetchHoldActivity), Arg.Any<object?>())
            .Returns(hold);

    private static void StubCurrency(WorkflowContext context) =>
        context.CallActivityAsync<EventPricing>(nameof(FetchEventPricingActivity), Arg.Any<object?>())
            .Returns(new EventPricing("INR", null, null));

    private static void StubCreateOrder(WorkflowContext context, bool alreadyExisted) =>
        context.CallActivityAsync<CreateOrderOutput>(nameof(CreateOrderActivity), Arg.Any<object?>())
            .Returns(new CreateOrderOutput(OrderId, 1000, "INR", alreadyExisted));

    private static void StubCreateIntent(WorkflowContext context)
    {
        context.CallActivityAsync<CreateIntentOutput>(nameof(CreateIntentActivity), Arg.Any<object?>())
            .Returns(new CreateIntentOutput("pi_test", "pi_test_secret"));
        context.CallActivityAsync<bool>(nameof(RecordPaymentIntentActivity), Arg.Any<object?>())
            .Returns(true);
    }

    private static void StubExtendHold(WorkflowContext context, DateTimeOffset expiresAt) =>
        context.CallActivityAsync<DateTimeOffset?>(nameof(ExtendHoldActivity), Arg.Any<object?>())
            .Returns(expiresAt);

    private static void StubPaymentOutcome(WorkflowContext context, PaymentOutcomeSignal signal)
    {
        context.WaitForExternalEventAsync<PaymentOutcomeSignal>("PaymentOutcome").Returns(signal);

        // The timer must never win the race in this scenario.
        context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<bool>().Task);
    }

    private static void StubNoPaymentOutcome(WorkflowContext context) =>
        context.WaitForExternalEventAsync<PaymentOutcomeSignal>("PaymentOutcome")
            .Returns(new TaskCompletionSource<PaymentOutcomeSignal>().Task);

    private static void StubTimerFires(WorkflowContext context) =>
        context.CreateTimer(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

    private static void StubSyncStatus(WorkflowContext context, string status) =>
        context.CallActivityAsync<string>(nameof(SyncPaymentStatusActivity), Arg.Any<object?>())
            .Returns(status);

    private static void StubConvert(WorkflowContext context, bool converted)
    {
        context.CallActivityAsync<bool>(nameof(ConvertActivity), Arg.Any<object?>()).Returns(converted);
        context.CallActivityAsync<bool>(nameof(ConfirmOrderActivity), Arg.Any<object?>()).Returns(true);
        context.CallActivityAsync<bool>(nameof(FailOrderActivity), Arg.Any<object?>()).Returns(true);
        context.CallActivityAsync<bool>(nameof(RefundActivity), Arg.Any<object?>()).Returns(true);
        context.CallActivityAsync<bool>(nameof(ReleaseHoldActivity), Arg.Any<object?>()).Returns(true);
    }
}
