namespace Catalog.Tests.Publishing;

// What a performance sells is no longer what the venue holds. A promoter hiring a hall closes
// blocks, renames them and caps a standing area, and the number that reaches Inventory and the
// storefront has to be the night's, not the building's. These are the cases where getting that
// wrong would be invisible: publishing succeeds either way, and only the capacity is silently a
// different number.
public sealed class SessionPublishCheckTests
{
    private const string LowerTier = "LT";
    private const string UpperTier = "UT";
    private const string Pit = "PIT";

    private static readonly DateTimeOffset Starts = DateTimeOffset.UtcNow.AddMonths(3);
    private static readonly DateTimeOffset Ends = Starts.AddHours(3);

    private readonly ITicketTypeRepository ticketTypes = Substitute.For<ITicketTypeRepository>();
    private readonly IVenueClient venue = Substitute.For<IVenueClient>();

    [Fact]
    public async Task AllocatingEveryBlock_ReportsTheWholeHouse()
    {
        var (@event, session) = GivenPerformance();
        var gold = GivenTicketType(@event, "Gold", 500_000);

        GivenSeatMap(session, Section(LowerTier, 800), Section(UpperTier, 400), Area(Pit, 600));
        session.SetAllocations([Sold(LowerTier, gold), Sold(UpperTier, gold), Sold(Pit, gold)]);

        var readiness = await RunAsync(session);

        readiness.Problem.ShouldBeNull();
        readiness.Capacity.ShouldBe(1_800);
        readiness.Allocations.Count.ShouldBe(3);
    }

    // The half-house show. The upper tier is closed, so it is not capacity — it is not on sale, and
    // nothing about it should reach Inventory or be counted as a seat this event could fill.
    [Fact]
    public async Task AnExcludedBlock_IsNeitherSoldNorCounted()
    {
        var (@event, session) = GivenPerformance();
        var gold = GivenTicketType(@event, "Gold", 500_000);

        GivenSeatMap(session, Section(LowerTier, 800), Section(UpperTier, 400));
        session.SetAllocations([Sold(LowerTier, gold), Excluded(UpperTier)]);

        var readiness = await RunAsync(session);

        readiness.Problem.ShouldBeNull();
        readiness.Capacity.ShouldBe(800);
        readiness.Allocations.Single().Code.ShouldBe(LowerTier);
    }

    // Selling 200 of a 400-capacity pit is a real thing a promoter does, and the smaller number is
    // the one Inventory must provision — not the area's own.
    [Fact]
    public async Task ACappedAdmissionArea_CountsWhatItSells_NotWhatItHolds()
    {
        var (@event, session) = GivenPerformance();
        var standing = GivenTicketType(@event, "Standing", 250_000);

        GivenSeatMap(session, Area(Pit, 400));
        session.SetAllocations([Sold(Pit, standing, capacityOverride: 200)]);

        var readiness = await RunAsync(session);

        readiness.Capacity.ShouldBe(200);
        readiness.Allocations.Single().CapacityOverride.ShouldBe(200);
    }

    [Fact]
    public async Task ABlockThatIsNeitherPricedNorExcluded_BlocksThePublish()
    {
        var (@event, session) = GivenPerformance();
        var gold = GivenTicketType(@event, "Gold", 500_000);

        GivenSeatMap(session, Section(LowerTier, 800), Section(UpperTier, 400));
        session.SetAllocations([Sold(LowerTier, gold)]);

        var readiness = await RunAsync(session);

        readiness.Problem.ShouldNotBeNull();

        // Null-conditional so this compiles without depending on whether the assertion above is
        // annotated as a null postcondition; the line before is what fails if it is null.
        readiness.Problem?.ShouldContain(UpperTier);
    }

    [Fact]
    public async Task ExcludingEveryBlock_BlocksThePublish()
    {
        var (_, session) = GivenPerformance();

        GivenSeatMap(session, Section(LowerTier, 800), Section(UpperTier, 400));
        session.SetAllocations([Excluded(LowerTier), Excluded(UpperTier)]);

        var readiness = await RunAsync(session);

        readiness.Problem.ShouldNotBeNull();
        readiness.Capacity.ShouldBe(0);
    }

    // An allocation left behind by an older layout names a block the pinned version does not have.
    // Counting it would add capacity nothing in the building backs, so it is ignored — the version
    // is what the loop walks.
    [Fact]
    public async Task AnAllocationForABlockTheVersionNoLongerHas_AddsNoCapacity()
    {
        var (@event, session) = GivenPerformance();
        var gold = GivenTicketType(@event, "Gold", 500_000);

        GivenSeatMap(session, Section(LowerTier, 800));
        session.SetAllocations([Sold(LowerTier, gold), Sold("GONE", gold)]);

        var readiness = await RunAsync(session);

        readiness.Problem.ShouldBeNull();
        readiness.Capacity.ShouldBe(800);
        readiness.Allocations.Single().Code.ShouldBe(LowerTier);
    }

    private static SessionAllocationSpec Sold(string code, TicketType type, int? capacityOverride = null) =>
        new(code, type.Id, IsExcluded: false, null, capacityOverride);

    private static SessionAllocationSpec Excluded(string code) => new(code, null, IsExcluded: true, null, null);

    private static SeatMapBlockSnapshot Section(string code, int seats) => new(code, seats, IsAdmissionArea: false);

    private static SeatMapBlockSnapshot Area(string code, int capacity) => new(code, capacity, IsAdmissionArea: true);

    private static (Event Event, EventSession Session) GivenPerformance()
    {
        var @event = Event.Create(
            Guid.CreateVersion7(),
            title: "ColdPlay India Tour — Mumbai",
            slug: $"coldplay-{Guid.CreateVersion7():N}",
            currency: "INR",
            startsAt: Starts,
            endsAt: Ends);

        return (@event, @event.Sessions.Single());
    }

    private TicketType GivenTicketType(Event @event, string name, long priceMinor)
    {
        var type = TicketType.Create(@event.Id, @event.TenantId, name, priceMinor);

        ticketTypes.ListForEventAsync(@event.Id, Arg.Any<CancellationToken>()).Returns([type]);
        return type;
    }

    // The performance pins the version it is checked against, so the substituted client has to
    // answer with the very ids the aggregate holds — otherwise every case fails on the
    // "no longer the published one" guard before reaching what it means to test.
    private void GivenSeatMap(EventSession session, params SeatMapBlockSnapshot[] blocks)
    {
        var seatMapId = Guid.CreateVersion7();
        var versionId = Guid.CreateVersion7();

        session.AttachSeatMap(
            Guid.CreateVersion7(),
            seatMapId,
            versionId,
            1,
            new VenueSnapshot("DY Patil Stadium", "Navi Mumbai", "IN", "Asia/Kolkata"));

        venue.GetSeatMapVersionAsync(seatMapId, 1, Arg.Any<CancellationToken>()).Returns(
            new SeatMapVersionSnapshot(
                seatMapId,
                session.VenueId!.Value,
                session.TenantId,
                versionId,
                1,
                IsPublished: true,
                blocks.Sum(b => b.Capacity),
                blocks.ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase),
                "DY Patil Stadium",
                "Navi Mumbai",
                "IN",
                "Asia/Kolkata"));
    }

    private Task<SessionPublishReadiness> RunAsync(EventSession session) =>
        SessionPublishCheck.RunAsync(session, ticketTypes, venue, CancellationToken.None);
}
