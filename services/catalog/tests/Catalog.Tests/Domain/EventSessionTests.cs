namespace Catalog.Tests.Domain;

// A performance is the grain every downstream service keys on, so its guards decide what Inventory
// provisions, what a ticket names and what a scanner accepts. Getting one wrong is not a display
// bug — it is capacity nobody can buy, or a ticket for a night that does not exist.
public sealed class EventSessionTests
{
    private static readonly DateTimeOffset Starts = new(2027, 3, 1, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ends = Starts.AddHours(4);

    [Fact]
    public void ANewPerformance_IsADraftThatSellsNothing()
    {
        var session = Draft();

        session.Status.ShouldBe(EventSessionStatus.Draft);
        session.IsSellable.ShouldBeFalse();
        session.SalesPaused.ShouldBeFalse();
    }

    [Fact]
    public void DoorsCannotOpenAfterThePerformanceStarts() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Draft().Reschedule(Starts, Ends, doorsOpenAt: Starts.AddMinutes(1), bookingEndsAt: null));

    // Selling a ticket once the doors are open is a different feature (walk-ups), not something the
    // cutoff should quietly allow by being set past the start.
    [Fact]
    public void ABookingCutoffAfterThePerformanceStarts_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Draft().Reschedule(Starts, Ends, doorsOpenAt: null, bookingEndsAt: Starts.AddMinutes(1)));

    [Fact]
    public void ABookingCutoffExactlyAtTheStartTime_IsAllowed()
    {
        var session = Draft();

        session.Reschedule(Starts, Ends, doorsOpenAt: null, bookingEndsAt: Starts);

        session.BookingEndsAt.ShouldBe(Starts);
    }

    [Fact]
    public void APerformanceIsSellableOnceItHasAMapAndAnAllocation()
    {
        var session = Draft();

        AttachMap(session);
        session.IsSellable.ShouldBeFalse();

        session.SetAllocations([Sold("LT")]);
        session.IsSellable.ShouldBeTrue();
    }

    [Fact]
    public void PublishingWithoutASeatMap_IsRefused()
    {
        var session = Draft();

        Should.Throw<InvalidOperationException>(session.Publish);
    }

    [Fact]
    public void PublishingWithNothingAllocated_IsRefused()
    {
        var session = Draft();
        AttachMap(session);

        Should.Throw<InvalidOperationException>(session.Publish);
    }

    // The whole reason the seat map is pinned by version: after this, the seats a sold ticket names
    // cannot move.
    [Fact]
    public void APublishedPerformance_CannotBeRescheduledOrRemapped()
    {
        var session = Sellable();
        session.Publish();

        Should.Throw<InvalidOperationException>(() =>
            session.Reschedule(Starts.AddDays(1), Ends.AddDays(1), null, null));

        Should.Throw<InvalidOperationException>(() => AttachMap(session));
        Should.Throw<InvalidOperationException>(() => session.SetAllocations([]));
    }

    // Allocations bind to codes that belong to one version of one map. Keeping the ones that happen
    // to match a different version would leave the rest silently missing, which surfaces much later
    // as capacity nobody can buy.
    [Fact]
    public void ChangingTheSeatMap_ClearsTheAllocations()
    {
        var session = Sellable();

        AttachMap(session);

        session.Allocations.ShouldBeEmpty();
        session.IsSellable.ShouldBeFalse();
    }

    [Fact]
    public void ReattachingTheSameVersion_KeepsTheAllocations()
    {
        var session = Draft();
        var versionId = Guid.CreateVersion7();

        AttachMap(session, versionId);
        session.SetAllocations([Sold("LT")]);

        AttachMap(session, versionId);

        session.Allocations.Count.ShouldBe(1);
    }

    [Fact]
    public void ABlockAllocatedTwice_IsRejected()
    {
        var session = Draft();
        AttachMap(session);

        Should.Throw<InvalidOperationException>(() => session.SetAllocations([Sold("LT"), Sold("lt")]));
    }

    // A promoter hiring half a stadium excludes the rest. That is a decision, so it is recorded
    // rather than left out — but it sells nothing, and a performance made entirely of them is an
    // empty house nobody could buy into.
    [Fact]
    public void APerformanceWithEveryBlockExcluded_SellsNothing()
    {
        var session = Draft();
        AttachMap(session);

        session.SetAllocations([Excluded("UT"), Excluded("LT")]);

        session.IsSellable.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(session.Publish);
    }

    [Fact]
    public void ExcludingSomeBlocks_StillLeavesThePerformanceSellable()
    {
        var session = Draft();
        AttachMap(session);

        session.SetAllocations([Sold("LT"), Excluded("UT")]);

        session.IsSellable.ShouldBeTrue();
        session.Publish();
        session.Status.ShouldBe(EventSessionStatus.Published);
    }

    // Priced and excluded are the two answers to one question, so a row carrying both — or neither —
    // is a caller that has not decided, and letting it through would sell a closed block or hide a
    // priced one without saying so.
    [Fact]
    public void ABlockThatIsBothPricedAndExcluded_IsRejected()
    {
        var session = Draft();
        AttachMap(session);

        Should.Throw<InvalidOperationException>(() => session.SetAllocations(
            [new SessionAllocationSpec("LT", Guid.CreateVersion7(), IsExcluded: true, null, null)]));

        Should.Throw<InvalidOperationException>(() => session.SetAllocations(
            [new SessionAllocationSpec("LT", null, IsExcluded: false, null, null)]));
    }

    // The code is what inventory, tickets and scanning bind to, so renaming a block for buyers has
    // to leave it alone — otherwise a rename is a data migration.
    [Fact]
    public void RenamingABlockForBuyers_LeavesItsCodeAlone()
    {
        var session = Draft();
        AttachMap(session);

        session.SetAllocations(
            [new SessionAllocationSpec("NS", Guid.CreateVersion7(), IsExcluded: false, "  Golden Circle  ", 250)]);

        var allocation = session.Allocations.Single();
        allocation.Code.ShouldBe("NS");
        allocation.DisplayName.ShouldBe("Golden Circle");
        allocation.CapacityOverride.ShouldBe(250);
    }

    [Fact]
    public void SalesCanOnlyBePausedOnAPublishedPerformance()
    {
        var session = Sellable();

        Should.Throw<InvalidOperationException>(session.PauseSales);

        session.Publish();
        session.PauseSales();
        session.SalesPaused.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(session.PauseSales);

        session.ResumeSales();
        session.SalesPaused.ShouldBeFalse();
    }

    [Fact]
    public void ACancelledPerformance_CannotBeCancelledAgain()
    {
        var session = Sellable();
        session.Publish();

        session.Cancel();
        session.Status.ShouldBe(EventSessionStatus.Cancelled);

        Should.Throw<InvalidOperationException>(session.Cancel);
    }

    [Fact]
    public void OverlapIsCheckedStrictly_SoTouchingRangesDoNotClash()
    {
        var session = Draft();

        session.Overlaps(Starts.AddHours(1), Ends.AddHours(1)).ShouldBeTrue();
        session.Overlaps(Ends, Ends.AddHours(2)).ShouldBeFalse();
        session.Overlaps(Starts.AddHours(-2), Starts).ShouldBeFalse();
    }

    private static EventSession Draft() =>
        Event.Create(
            Guid.CreateVersion7(),
            title: "ColdPlay India Tour — Mumbai",
            slug: "coldplay-india-tour-mumbai",
            currency: "INR",
            startsAt: Starts,
            endsAt: Ends)
            .Sessions.Single();

    private static EventSession Sellable()
    {
        var session = Draft();
        AttachMap(session);
        session.SetAllocations([Sold("LT")]);

        return session;
    }

    private static SessionAllocationSpec Sold(string code) =>
        new(code, Guid.CreateVersion7(), IsExcluded: false, null, null);

    private static SessionAllocationSpec Excluded(string code) =>
        new(code, null, IsExcluded: true, null, null);

    private static void AttachMap(EventSession session, Guid? versionId = null) =>
        session.AttachSeatMap(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            versionId ?? Guid.CreateVersion7(),
            1,
            new VenueSnapshot("DY Patil Stadium", "Navi Mumbai", "IN", "Asia/Kolkata"));
}
