namespace Inventory.Tests.Holds;

// PlaceHoldAsync runs a sequence of organizer-configured gates before it touches any inventory.
// Each one must reject with its own outcome — the API maps them to distinct responses, and a buyer
// told "sold out" when the truth is "not on sale yet" is a support ticket. Each test also asserts
// the Redis fast gate was never reached: these checks are cheap and must stay in front of it, since
// a rejected request should cost nothing under the load these gates exist to survive.
public sealed class PlaceHoldGateTests
{
    private static readonly Guid EventId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid SeatId = Guid.CreateVersion7();

    private readonly IInventoryRepository inventory = Substitute.For<IInventoryRepository>();
    private readonly IHoldStore holdStore = Substitute.For<IHoldStore>();
    private readonly IEventPublisher events = Substitute.For<IEventPublisher>();
    private readonly IQueueAdmissionTokenValidator queueTokens = Substitute.For<IQueueAdmissionTokenValidator>();

    [Fact]
    public async Task RequestingNeitherSeatsNorAdmissions_IsRejectedWithoutLoadingAnything()
    {
        var result = await PlaceHoldAsync(seatIds: []);

        result.Outcome.ShouldBe(PlaceHoldOutcome.SeatNotFound);
        await inventory.DidNotReceive().GetEventInventorySettingsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnEventInventoryHasNeverBeenProvisionedFor_IsNotFound()
    {
        GivenSettings(null);

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldBe(PlaceHoldOutcome.EventNotFound);
        await ShouldNotHaveReachedTheFastGate();
    }

    [Fact]
    public async Task AnEventWithSalesPaused_IsRejected()
    {
        var settings = GivenSettings(CreateSettings());
        settings!.SetSalesPaused(true);

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldBe(PlaceHoldOutcome.SalesPaused);
        await ShouldNotHaveReachedTheFastGate();
    }

    [Fact]
    public async Task BeforeTheOnSaleTime_IsRejected()
    {
        GivenSettings(CreateSettings(onSaleAt: DateTimeOffset.UtcNow.AddHours(1)));

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldBe(PlaceHoldOutcome.OnSaleNotStarted);
        await ShouldNotHaveReachedTheFastGate();
    }

    [Fact]
    public async Task OnceTheOnSaleTimeHasPassed_TheGateOpens()
    {
        GivenSettings(CreateSettings(onSaleAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldNotBe(PlaceHoldOutcome.OnSaleNotStarted);
        await ShouldHaveGoneOnToLoadInventory();
    }

    [Fact]
    public async Task AfterTheBookingCutoff_IsRejected()
    {
        GivenSettings(CreateSettings(bookingEndsAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldBe(PlaceHoldOutcome.BookingWindowClosed);
        await ShouldNotHaveReachedTheFastGate();
    }

    [Fact]
    public async Task WithinTheBookingWindow_TheGateOpens()
    {
        GivenSettings(CreateSettings(bookingEndsAt: DateTimeOffset.UtcNow.AddHours(1)));

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldNotBe(PlaceHoldOutcome.BookingWindowClosed);
        await ShouldHaveGoneOnToLoadInventory();
    }

    // The per-buyer limit counts what the buyer already holds or has bought for this event, not just
    // what this one request asks for — otherwise splitting a request across several orders defeats it.
    [Fact]
    public async Task ARequestThatWouldExceedTheBuyerLimitCountingEarlierOrders_IsRejected()
    {
        GivenSettings(CreateSettings(maxTicketsPerBuyer: 4));
        GivenBuyerAlreadyCommitted(3);

        var result = await PlaceHoldAsync(generalAdmissionSelections: [(Guid.CreateVersion7(), 2)]);

        result.Outcome.ShouldBe(PlaceHoldOutcome.BuyerLimitExceeded);
        await ShouldNotHaveReachedTheFastGate();
    }

    [Fact]
    public async Task ARequestThatLandsExactlyOnTheBuyerLimit_IsAllowed()
    {
        GivenSettings(CreateSettings(maxTicketsPerBuyer: 4));
        GivenBuyerAlreadyCommitted(3);

        var result = await PlaceHoldAsync();

        result.Outcome.ShouldNotBe(PlaceHoldOutcome.BuyerLimitExceeded);
        await ShouldHaveGoneOnToLoadInventory();
    }

    [Fact]
    public async Task WithNoBuyerLimitConfigured_TheBuyersHistoryIsNotEvenQueried()
    {
        GivenSettings(CreateSettings(maxTicketsPerBuyer: null));

        await PlaceHoldAsync();

        await inventory.DidNotReceive()
            .GetBuyerCommittedQuantityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnAQueuedEvent_AHoldWithNoAdmissionToken_IsRejected()
    {
        GivenSettings(CreateSettings(requiresQueue: true));
        queueTokens.IsValid(null, EventId, Arg.Any<DateTimeOffset>()).Returns(false);

        var result = await PlaceHoldAsync(queueAdmissionToken: null);

        result.Outcome.ShouldBe(PlaceHoldOutcome.QueueAdmissionRequired);
        await ShouldNotHaveReachedTheFastGate();
    }

    [Fact]
    public async Task OnAQueuedEvent_AValidAdmissionToken_OpensTheGate()
    {
        GivenSettings(CreateSettings(requiresQueue: true));
        queueTokens.IsValid("admitted", EventId, Arg.Any<DateTimeOffset>()).Returns(true);

        var result = await PlaceHoldAsync(queueAdmissionToken: "admitted");

        result.Outcome.ShouldNotBe(PlaceHoldOutcome.QueueAdmissionRequired);
        await ShouldHaveGoneOnToLoadInventory();
    }

    // An event that never opted into the waiting room must not start demanding tokens — this is the
    // regression guard for the queue feature being additive rather than breaking every other event.
    [Fact]
    public async Task OnAnUnqueuedEvent_NoAdmissionTokenIsRequiredOrEvenChecked()
    {
        GivenSettings(CreateSettings(requiresQueue: false));

        var result = await PlaceHoldAsync(queueAdmissionToken: null);

        result.Outcome.ShouldNotBe(PlaceHoldOutcome.QueueAdmissionRequired);
        queueTokens.DidNotReceive().IsValid(Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>());
    }

    private static EventInventorySettings CreateSettings(
        DateTimeOffset? bookingEndsAt = null,
        int? maxTicketsPerBuyer = null,
        DateTimeOffset? onSaleAt = null,
        bool requiresQueue = false) =>
        EventInventorySettings.Create(
            EventId,
            tenantId: Guid.CreateVersion7(),
            bookingEndsAt,
            maxTicketsPerBuyer,
            onSaleAt,
            requiresQueue);

    private EventInventorySettings? GivenSettings(EventInventorySettings? settings)
    {
        inventory.GetEventInventorySettingsAsync(EventId, Arg.Any<CancellationToken>()).Returns(settings);
        return settings;
    }

    private void GivenBuyerAlreadyCommitted(int quantity) =>
        inventory.GetBuyerCommittedQuantityAsync(EventId, UserId, Arg.Any<CancellationToken>()).Returns(quantity);

    // The gates run before any inventory is loaded, so reaching this call is what "the gate opened"
    // means. The request still fails afterwards (no seats are stubbed), which is deliberate: these
    // tests are about the gates, not about a successful hold.
    private async Task ShouldHaveGoneOnToLoadInventory() =>
        await inventory.Received(1).GetItemsBySeatsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());

    private async Task ShouldNotHaveReachedTheFastGate()
    {
        await inventory.DidNotReceive().GetItemsBySeatsAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>());
        await holdStore.DidNotReceive().TryHoldAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyList<Guid>>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    private async Task<PlaceHoldResult> PlaceHoldAsync(
        IReadOnlyList<Guid>? seatIds = null,
        IReadOnlyList<(Guid AllocationId, int Quantity)>? generalAdmissionSelections = null,
        string? queueAdmissionToken = null)
    {
        var service = new HoldService(inventory, holdStore, events, new HoldOptions(), queueTokens);

        return await service.PlaceHoldAsync(
            UserId,
            EventId,
            seatIds ?? [SeatId],
            generalAdmissionSelections ?? [],
            queueAdmissionToken,
            CancellationToken.None);
    }
}
