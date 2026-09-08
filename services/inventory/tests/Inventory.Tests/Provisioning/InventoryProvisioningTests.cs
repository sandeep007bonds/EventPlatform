namespace Inventory.Tests.Provisioning;

// Provisioning is where two services' answers are joined by block code, and it is the last moment a
// mistake is cheap: after this, wrong inventory is a wrong sale. What a performance sells is not
// what the venue holds — it may close a block, or sell fewer of a standing area than it fits — and
// both of those arrive as an absence or a smaller number rather than as anything loud.
public sealed class InventoryProvisioningTests
{
    private const string LowerTier = "LT";
    private const string UpperTier = "UT";
    private const string Pit = "PIT";

    private static readonly Guid SeatMapId = Guid.CreateVersion7();
    private static readonly Guid EventSessionId = Guid.CreateVersion7();
    private static readonly Guid CatalogEventId = Guid.CreateVersion7();
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid TicketTypeId = Guid.CreateVersion7();

    private readonly IInventoryRepository inventory = Substitute.For<IInventoryRepository>();
    private readonly ISeatMapClient seatMaps = Substitute.For<ISeatMapClient>();
    private readonly IHoldStore holdStore = Substitute.For<IHoldStore>();

    // The half-house show: Catalog leaves the excluded block out of the payload entirely, so the
    // only thing Inventory sees is a seat whose code it cannot find. Skipping it is what makes the
    // block genuinely unsellable rather than merely hidden by the storefront.
    [Fact]
    public async Task SeatsInABlockTheEventExcluded_AreNotProvisioned()
    {
        GivenSeatMap(
            seats: [Seat(LowerTier), Seat(LowerTier), Seat(UpperTier)],
            areas: []);

        var result = await ProvisionAsync(Allocation(LowerTier));

        result.SeatCount.ShouldBe(2);
        inventory.Received(1).AddRange(Arg.Is<IEnumerable<InventoryItem>>(items => items.Count() == 2));
    }

    // Selling 200 of a 400-capacity pit. Venue still says 400 — that is the building — so the
    // smaller number has to come from the allocation, and it has to reach both Postgres and the
    // Redis counter, or the fast gate would sell the 400 the pool was never given.
    [Fact]
    public async Task ACappedAdmissionArea_ProvisionsWhatItSells_NotWhatItHolds()
    {
        GivenSeatMap(seats: [], areas: [Area(Pit, 400)]);

        var result = await ProvisionAsync(Allocation(Pit, capacityOverride: 200));

        result.GeneralAdmissionAllocationCount.ShouldBe(1);

        var pool = CapturedPools().Single();
        pool.TotalCapacity.ShouldBe(200);

        await holdStore.Received(1).InitializeGeneralAdmissionCapacityAsync(
            EventSessionId,
            pool.Id,
            200,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnUncappedAdmissionArea_ProvisionsEverythingTheVenueHolds()
    {
        GivenSeatMap(seats: [], areas: [Area(Pit, 400)]);

        await ProvisionAsync(Allocation(Pit));

        CapturedPools().Single().TotalCapacity.ShouldBe(400);
    }

    private static SeatSnapshot Seat(string sectionCode) => new(Guid.CreateVersion7(), sectionCode, IsSellable: true);

    private static AdmissionAreaSnapshot Area(string code, int capacity) => new(Guid.CreateVersion7(), code, capacity);

    private static SessionAllocationContract Allocation(string code, int? capacityOverride = null) =>
        new(code, TicketTypeId, 500_000, capacityOverride);

    private IReadOnlyList<GeneralAdmissionAllocation> CapturedPools() =>
        inventory.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IInventoryRepository.AddGeneralAdmissionAllocations))
            .SelectMany(call => (IEnumerable<GeneralAdmissionAllocation>)call.GetArguments()[0]!)
            .ToList();

    private void GivenSeatMap(IReadOnlyList<SeatSnapshot> seats, IReadOnlyList<AdmissionAreaSnapshot> areas) =>
        seatMaps.GetSeatMapAsync(SeatMapId, 1, Arg.Any<CancellationToken>())
            .Returns(new SeatMapSnapshot(seats, areas));

    private Task<ProvisioningResult> ProvisionAsync(params SessionAllocationContract[] allocations) =>
        new InventoryProvisioningService(inventory, seatMaps, holdStore).ProvisionAsync(
            new ProvisionSessionRequest(
                TenantId,
                EventSessionId,
                CatalogEventId,
                SeatMapId,
                SeatMapVersionNumber: 1,
                BookingEndsAt: null,
                OnSaleAt: null,
                MaxTicketsPerBuyer: null,
                RequiresQueue: false,
                allocations),
            CancellationToken.None);
}
