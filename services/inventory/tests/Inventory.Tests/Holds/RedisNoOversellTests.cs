namespace Inventory.Tests.Holds;

// The guarantee the whole product rests on, proved against a real Redis rather than argued from
// reading the Lua. Everything else in this repo can be wrong and recoverable; selling the same seat
// to two people is not. Redis runs commands single-threaded, so a script is atomic — but that is a
// property of how these scripts are written (check every seat, then write every seat, in one
// script), not something the runtime grants to any code that happens to live in Redis. If someone
// later splits the check and the write into two round trips, these tests are what notices.
//
// IHoldStore is resolved through the service's own registration rather than constructed directly,
// so a broken DI wiring fails here too, and because RedisHoldStore is internal to the service.
public sealed class RedisNoOversellTests : IAsyncLifetime
{
    private const int Contenders = 25;

    private readonly RedisContainer redis = new RedisBuilder().Build();
    private ServiceProvider provider = default!;
    private IHoldStore holdStore = default!;

    public async Task InitializeAsync()
    {
        await redis.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = redis.GetConnectionString(),

                // Never connected to: resolving IHoldStore touches only the Redis multiplexer.
                ["ConnectionStrings:inventory"] = "Host=localhost;Database=inventory;Username=u;Password=p",
            })
            .Build();

        provider = new ServiceCollection()
            .AddLogging()
            .AddInventoryInfrastructure(configuration)
            .BuildServiceProvider();

        holdStore = provider.GetRequiredService<IHoldStore>();
    }

    public async Task DisposeAsync()
    {
        await provider.DisposeAsync();
        await redis.DisposeAsync();
    }

    // The headline case: one seat, many simultaneous buyers.
    [Fact]
    public async Task ManyBuyersRacingForOneSeat_ExactlyOneWins()
    {
        var eventId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();

        var results = await RaceAsync(_ => TryHoldSeatsAsync(eventId, [seatId]));

        results.Count(result => result.Success).ShouldBe(1);
        results.Where(result => !result.Success)
            .ShouldAllBe(result => result.ConflictSeatId == seatId);
    }

    // The subtler oversell: 25 buyers, 5 seats, each asking for one. Redis must serialise them into
    // exactly five winners — not five *attempts*, five successes.
    [Fact]
    public async Task MoreBuyersThanSeats_NoMoreThanTheSeatsAreSold()
    {
        var eventId = Guid.CreateVersion7();
        var seatIds = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();

        var results = await RaceAsync(attempt => TryHoldSeatsAsync(eventId, [seatIds[attempt % seatIds.Count]]));

        results.Count(result => result.Success).ShouldBe(seatIds.Count);
    }

    // A multi-seat hold is all-or-nothing. A loser that came away holding *some* of its seats would
    // strand them: no hold record owns them, so nothing releases them and they are lost until the
    // reconciler runs. This is the failure a per-seat loop instead of one script would produce.
    [Fact]
    public async Task WhenTwoMultiSeatHoldsOverlap_TheLoserHoldsNothing()
    {
        var eventId = Guid.CreateVersion7();
        var shared = Guid.CreateVersion7();
        var onlyMine = Guid.CreateVersion7();
        var onlyYours = Guid.CreateVersion7();

        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var results = await RaceAsync(
            attempt => attempt % 2 == 0
                ? TryHoldSeatsAsync(eventId, [shared, onlyMine], first)
                : TryHoldSeatsAsync(eventId, [shared, onlyYours], second));

        results.Count(result => result.Success).ShouldBe(1);

        // Whichever hold lost, its exclusive seat must still be free for someone else to take.
        var loserExclusiveSeat = await TryHoldSeatsAsync(eventId, [onlyMine]);
        var otherExclusiveSeat = await TryHoldSeatsAsync(eventId, [onlyYours]);
        (loserExclusiveSeat.Success ^ otherExclusiveSeat.Success).ShouldBeTrue(
            "exactly one of the two exclusive seats belongs to the winning hold and is taken; " +
            "the loser's must have been left untouched");
    }

    [Fact]
    public async Task ReleasingAHold_PutsTheSeatBackInPlay()
    {
        var eventId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();
        var holdId = Guid.CreateVersion7();

        (await TryHoldSeatsAsync(eventId, [seatId], holdId)).Success.ShouldBeTrue();
        (await TryHoldSeatsAsync(eventId, [seatId])).Success.ShouldBeFalse();

        await holdStore.ReleaseAsync(eventId, holdId, [seatId], CancellationToken.None);

        (await TryHoldSeatsAsync(eventId, [seatId])).Success.ShouldBeTrue();
    }

    [Fact]
    public async Task ASoldSeat_StaysSoldEvenAfterItsHoldIsReleased()
    {
        var eventId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();
        var holdId = Guid.CreateVersion7();

        (await TryHoldSeatsAsync(eventId, [seatId], holdId)).Success.ShouldBeTrue();
        await holdStore.MarkSoldAsync(eventId, holdId, [seatId], CancellationToken.None);
        await holdStore.ReleaseAsync(eventId, holdId, [seatId], CancellationToken.None);

        (await TryHoldSeatsAsync(eventId, [seatId])).Success.ShouldBeFalse();
    }

    // The counter-based half of no-oversell, under the same contention. A decrement that read and
    // wrote in separate round trips would let several buyers past the same remaining count.
    [Fact]
    public async Task ManyBuyersRacingForLimitedGeneralAdmission_NeverExceedTheCapacity()
    {
        var eventId = Guid.CreateVersion7();
        var allocationId = Guid.CreateVersion7();
        const int capacity = 7;
        await holdStore.InitializeGeneralAdmissionCapacityAsync(
            eventId, allocationId, capacity, CancellationToken.None);

        var results = await RaceAsync(_ => TryHoldAdmissionsAsync(eventId, allocationId, quantity: 1));

        results.Count(result => result.Success).ShouldBe(capacity);
        results.Where(result => !result.Success)
            .ShouldAllBe(result => result.ConflictAllocationId == allocationId);
    }

    // Same capacity, but each buyer wants two — the count of winners has to respect the quantity
    // asked for, not just the number of requests.
    [Fact]
    public async Task GeneralAdmissionRespectsRequestedQuantity_NotJustRequestCount()
    {
        var eventId = Guid.CreateVersion7();
        var allocationId = Guid.CreateVersion7();
        const int capacity = 7;
        await holdStore.InitializeGeneralAdmissionCapacityAsync(
            eventId, allocationId, capacity, CancellationToken.None);

        var results = await RaceAsync(_ => TryHoldAdmissionsAsync(eventId, allocationId, quantity: 2));

        // 7 capacity, 2 per buyer -> 3 winners, and one admission left stranded.
        results.Count(result => result.Success).ShouldBe(capacity / 2);
    }

    [Fact]
    public async Task RequestingMoreAdmissionsThanTheSectionHolds_IsRejected()
    {
        var eventId = Guid.CreateVersion7();
        var allocationId = Guid.CreateVersion7();
        await holdStore.InitializeGeneralAdmissionCapacityAsync(
            eventId, allocationId, totalCapacity: 3, CancellationToken.None);

        var result = await TryHoldAdmissionsAsync(eventId, allocationId, quantity: 4);

        result.Success.ShouldBeFalse();
        result.ConflictAllocationId.ShouldBe(allocationId);

        // And the failed attempt must not have eaten into the capacity on its way out.
        (await TryHoldAdmissionsAsync(eventId, allocationId, quantity: 3)).Success.ShouldBeTrue();
    }

    private Task<HoldStoreResult> TryHoldSeatsAsync(Guid eventId, Guid[] seatIds, Guid? holdId = null) =>
        holdStore.TryHoldAsync(
            eventId,
            holdId ?? Guid.CreateVersion7(),
            seatIds,
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

    private Task<GeneralAdmissionHoldStoreResult> TryHoldAdmissionsAsync(
        Guid eventId,
        Guid allocationId,
        int quantity) =>
        holdStore.TryHoldGeneralAdmissionAsync(
            eventId,
            Guid.CreateVersion7(),
            [(allocationId, quantity)],
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

    // Starts every attempt and releases them at once. Without the gate the tasks would trickle out
    // as the thread pool schedules them and mostly not overlap at all — the test would still pass
    // and would prove nothing.
    private static async Task<IReadOnlyList<TResult>> RaceAsync<TResult>(Func<int, Task<TResult>> attempt)
    {
        using var gate = new SemaphoreSlim(0, Contenders);

        var racers = Enumerable.Range(0, Contenders)
            .Select(async index =>
            {
                await gate.WaitAsync();
                return await attempt(index);
            })
            .ToList();

        gate.Release(Contenders);

        return await Task.WhenAll(racers);
    }
}
