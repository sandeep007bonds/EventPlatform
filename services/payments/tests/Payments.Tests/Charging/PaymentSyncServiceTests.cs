namespace Payments.Tests.Charging;

/// <summary>
/// Covers the reconciliation that runs when nobody told us how a payment ended — the pull path the
/// checkout saga polls, and the sweep that closes out payments a buyer abandoned (ADR-0028). These
/// are the paths where getting it wrong means either charging someone for nothing or failing a
/// payment that is holding their money, so the assertions are about *money safety* first.
/// </summary>
public sealed class PaymentSyncServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IPaymentRepository payments = Substitute.For<IPaymentRepository>();
    private readonly IPaymentGateway gateway = Substitute.For<IPaymentGateway>();
    private readonly IEventPublisher events = Substitute.For<IEventPublisher>();

    [Fact]
    public async Task SyncAsync_WhenProviderReportsCapture_MarksCapturedAndPublishes()
    {
        var payment = CreateInitiatedPayment();
        StubLatest(payment);
        gateway.GetStatusAsync("pi_test", Arg.Any<CancellationToken>()).Returns(GatewayPaymentStatus.Captured);

        var result = await CreateService().SyncAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.Captured);
        payment.Status.ShouldBe(PaymentStatus.Captured);
        events.Received(1).Enqueue(Arg.Any<PaymentCaptured>());
        await payments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // The webhook and the poll can both observe the same capture. Whichever is second must be a
    // no-op — a second PaymentCaptured would re-run the saga and could issue duplicate tickets.
    [Fact]
    public async Task SyncAsync_WhenAlreadyCaptured_PublishesNothingAndAsksNoProvider()
    {
        var payment = CreateInitiatedPayment();
        payment.TryMarkCaptured("pi_test").ShouldBeTrue();
        StubLatest(payment);

        var result = await CreateService().SyncAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.Captured);
        events.DidNotReceive().Enqueue(Arg.Any<IntegrationEvent>());
        await gateway.DidNotReceive().GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_WhileTheBuyerIsStillAuthenticating_ReportsPendingAndChangesNothing()
    {
        var payment = CreateInitiatedPayment();
        StubLatest(payment);
        gateway.GetStatusAsync("pi_test", Arg.Any<CancellationToken>()).Returns(GatewayPaymentStatus.Pending);

        var result = await CreateService().SyncAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.Pending);
        payment.Status.ShouldBe(PaymentStatus.Initiated);
        events.DidNotReceive().Enqueue(Arg.Any<IntegrationEvent>());
    }

    [Fact]
    public async Task AbandonAsync_CancelsAtTheProviderThenFailsAndPublishes()
    {
        var payment = CreateInitiatedPayment();
        StubLatest(payment);
        gateway.TryCancelAsync("pi_test", Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateService().AbandonAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.Failed);
        payment.Status.ShouldBe(PaymentStatus.Failed);
        payment.FailureReason.ShouldBe("payment_abandoned");
        await gateway.Received(1).TryCancelAsync("pi_test", Arg.Any<CancellationToken>());
        events.Received(1).Enqueue(Arg.Any<PaymentFailed>());
    }

    // The one that actually matters. A refused cancellation nearly always means the buyer completed
    // the payment after our last read — so the sweep must re-read, never assume abandonment.
    // Marking this failed would strand real money and release seats that were genuinely bought.
    [Fact]
    public async Task AbandonAsync_WhenTheProviderRefusesTheCancel_ReReadsRatherThanFailing()
    {
        var payment = CreateInitiatedPayment();
        StubLatest(payment);
        gateway.TryCancelAsync("pi_test", Arg.Any<CancellationToken>()).Returns(false);
        gateway.GetStatusAsync("pi_test", Arg.Any<CancellationToken>()).Returns(GatewayPaymentStatus.Captured);

        var result = await CreateService().AbandonAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.Captured);
        payment.Status.ShouldBe(PaymentStatus.Captured);
        payment.FailureReason.ShouldBeNull();
        events.Received(1).Enqueue(Arg.Any<PaymentCaptured>());
        events.DidNotReceive().Enqueue(Arg.Any<PaymentFailed>());
    }

    [Fact]
    public async Task AbandonAsync_WhenNoIntentWasEverCreated_FailsWithoutCallingTheProvider()
    {
        // Intent creation threw before a reference came back: nothing exists provider-side to
        // cancel, but the payment is still dead and must be closed out so the order can unwind.
        var payment = Payment.Create(TenantId, OrderId, "stripe", "idem-key-1", 1000, "INR");
        StubLatest(payment);

        var result = await CreateService().AbandonAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.Failed);
        payment.Status.ShouldBe(PaymentStatus.Failed);
        await gateway.DidNotReceive().TryCancelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        events.Received(1).Enqueue(Arg.Any<PaymentFailed>());
    }

    [Fact]
    public async Task SyncAsync_WhenNoPaymentExistsForTheOrder_ReportsNotFound()
    {
        payments.GetLatestByOrderAsync(OrderId, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var result = await CreateService().SyncAsync(OrderId, CancellationToken.None);

        result.ShouldBe(PaymentSyncResult.NotFound);
    }

    private static Payment CreateInitiatedPayment()
    {
        var payment = Payment.Create(TenantId, OrderId, "stripe", "idem-key-1", 1000, "INR");
        payment.RecordIntentDetails("pi_test", "pi_test_secret");
        return payment;
    }

    private PaymentSyncService CreateService() => new(payments, gateway, events);

    private void StubLatest(Payment payment) =>
        payments.GetLatestByOrderAsync(OrderId, Arg.Any<CancellationToken>()).Returns(payment);
}
