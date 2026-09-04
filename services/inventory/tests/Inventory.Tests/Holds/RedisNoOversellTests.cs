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

    // Pinned to the image docker-compose runs, not the Testcontainers module default. Two reasons,
    // and the second is why this file changed: the default is a different Redis build than production
    // uses, so the tests were proving the wrong version; and the default is an image nothing else
    // pulls, so it is never in the local cache and every run depends on a registry fetch that can
    // rate-limit or fail. Keep this in step with docker-compose.yml.
    private readonly RedisContainer redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

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
        var eventSessionId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();

        var results = await RaceAsync(_ => TryHoldSeatsAsync(eventSessionId, [seatId]));

        results.Count(result => result.Success).ShouldBe(1);
        results.Where(result => !result.Success)
            .ShouldAllBe(result => result.ConflictSeatId == seatId);
    }

    // The subtler oversell: 25 buyers, 5 seats, each asking for one. Redis must serialise them into
    // exactly five winners — not five *attempts*, five successes.
    [Fact]
    public async Task MoreBuyersThanSeats_NoMoreThanTheSeatsAreSold()
    {
        var eventSessionId = Guid.CreateVersion7();
        var seatIds = Enumerable.Range(0, 5).Select(_ => Guid.CreateVersion7()).ToList();

        var results = await RaceAsync(attempt => TryHoldSeatsAsync(eventSessionId, [seatIds[attempt % seatIds.Count]]));

        results.Count(result => result.Success).ShouldBe(seatIds.Count);
    }

    // A multi-seat hold is all-or-nothing. A loser that came away holding *some* of its seats would
    // strand them: no hold record owns them, so nothing releases them and they are lost until the
    // reconciler runs. This is the failure a per-seat loop instead of one script would produce.
    [Fact]
    public async Task WhenTwoMultiSeatHoldsOverlap_TheLoserHoldsNothing()
    {
        var eventSessionId = Guid.CreateVersion7();
        var shared = Guid.CreateVersion7();
        var onlyMine = Guid.CreateVersion7();
        var onlyYours = Guid.CreateVersion7();

        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        var results = await RaceAsync(
            attempt => attempt % 2 == 0
                ? TryHoldSeatsAsync(eventSessionId, [shared, onlyMine], first)
                : TryHoldSeatsAsync(eventSessionId, [shared, onlyYours], second));

        results.Count(result => result.Success).ShouldBe(1);

        // Whichever hold lost, its exclusive seat must still be free for someone else to take.
        var loserExclusiveSeat = await TryHoldSeatsAsync(eventSessionId, [onlyMine]);
        var otherExclusiveSeat = await TryHoldSeatsAsync(eventSessionId, [onlyYours]);
        (loserExclusiveSeat.Success ^ otherExclusiveSeat.Success).ShouldBeTrue(
            "exactly one of the two exclusive seats belongs to the winning hold and is taken; " +
            "the loser's must have been left untouched");
    }

    [Fact]
    public async Task ReleasingAHold_PutsTheSeatBackInPlay()
    {
        var eventSessionId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();
        var holdId = Guid.CreateVersion7();

        (await TryHoldSeatsAsync(eventSessionId, [seatId], holdId)).Success.ShouldBeTrue();
        (await TryHoldSeatsAsync(eventSessionId, [seatId])).Success.ShouldBeFalse();

        await holdStore.ReleaseAsync(eventSessionId, holdId, [seatId], CancellationToken.None);

        (await TryHoldSeatsAsync(eventSessionId, [seatId])).Success.ShouldBeTrue();
    }

    [Fact]
    public async Task ASoldSeat_StaysSoldEvenAfterItsHoldIsReleased()
    {
        var eventSessionId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();
        var holdId = Guid.CreateVersion7();

        (await TryHoldSeatsAsync(eventSessionId, [seatId], holdId)).Success.ShouldBeTrue();
        await holdStore.MarkSoldAsync(eventSessionId, holdId, [seatId], CancellationToken.None);
        await holdStore.ReleaseAsync(eventSessionId, holdId, [seatId], CancellationToken.None);

        (await TryHoldSeatsAsync(eventSessionId, [seatId])).Success.ShouldBeFalse();
    }

    // The failure mode the grain change (ADR-0039) introduced, and the reason every Redis key is
    // now scoped to the performance. A seat id is a *Venue* seat — the same physical chair on
    // Friday and on Saturday — so a key scoped to the event would make holding A1 for one night
    // mark it taken for the whole run. That is not an oversell, so nothing else in this file would
    // catch it: it silently stops selling seats that are free.
    [Fact]
    public async Task HoldingASeatForOnePerformance_LeavesTheSameSeatFreeForAnother()
    {
        var fridayId = Guid.CreateVersion7();
        var saturdayId = Guid.CreateVersion7();
        var seatId = Guid.CreateVersion7();

        (await TryHoldSeatsAsync(fridayId, [seatId])).Success.ShouldBeTrue();

        (await TryHoldSeatsAsync(saturdayId, [seatId])).Success.ShouldBeTrue(
            "the same seat on a different night is different inventory");
    }

    // The counter-based half of no-oversell, under the same contention. A decrement that read and
    // wrote in separate round trips would let several buyers past the same remaining count.
    [Fact]
    public async Task ManyBuyersRacingForLimitedGeneralAdmission_NeverExceedTheCapacity()
    {
        var eventSessionId = Guid.CreateVersion7();
        var allocationId = Guid.CreateVersion7();
        const int capacity = 7;
        await holdStore.InitializeGeneralAdmissionCapacityAsync(
            eventSessionId, allocationId, capacity, CancellationToken.None);

        var results = await RaceAsync(_ => TryHoldAdmissionsAsync(eventSessionId, allocationId, quantity: 1));

        results.Count(result => result.Success).ShouldBe(capacity);
        results.Where(result => !result.Success)
            .ShouldAllBe(result => result.ConflictAllocationId == allocationId);
    }

    // Same capacity, but each buyer wants two — the count of winners has to respect the quantity
    // asked for, not just the number of requests.
    [Fact]
    public async Task GeneralAdmissionRespectsRequestedQuantity_NotJustRequestCount()
    {
        var eventSessionId = Guid.CreateVersion7();
        var allocationId = Guid.CreateVersion7();
        const int capacity = 7;
        await holdStore.InitializeGeneralAdmissionCapacityAsync(
            eventSessionId, allocationId, capacity, CancellationToken.None);

        var results = await RaceAsync(_ => TryHoldAdmissionsAsync(eventSessionId, allocationId, quantity: 2));

        // 7 capacity, 2 per buyer -> 3 winners, and one admission left stranded.
        results.Count(result => result.Success).ShouldBe(capacity / 2);
    }

    [Fact]
    public async Task RequestingMoreAdmissionsThanTheSectionHolds_IsRejected()
    {
        var eventSessionId = Guid.CreateVersion7();
        var allocationId = Guid.CreateVersion7();
        await holdStore.InitializeGeneralAdmissionCapacityAsync(
            eventSessionId, allocationId, totalCapacity: 3, CancellationToken.None);

        var result = await TryHoldAdmissionsAsync(eventSessionId, allocationId, quantity: 4);

        result.Success.ShouldBeFalse();
        result.ConflictAllocationId.ShouldBe(allocationId);

        // And the failed attempt must not have eaten into the capacity on its way out.
        (await TryHoldAdmissionsAsync(eventSessionId, allocationId, quantity: 3)).Success.ShouldBeTrue();
    }

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

    private Task<HoldStoreResult> TryHoldSeatsAsync(Guid eventSessionId, Guid[] seatIds, Guid? holdId = null) =>
        holdStore.TryHoldAsync(
            eventSessionId,
            holdId ?? Guid.CreateVersion7(),
            seatIds,
            TimeSpan.FromMinutes(2),
            CancellationToken.None);

    private Task<GeneralAdmissionHoldStoreResult> TryHoldAdmissionsAsync(
        Guid eventSessionId,
        Guid allocationId,
        int quantity) =>
        holdStore.TryHoldGeneralAdmissionAsync(
            eventSessionId,
            Guid.CreateVersion7(),
            [(allocationId, quantity)],
            TimeSpan.FromMinutes(2),
            CancellationToken.None);
}
