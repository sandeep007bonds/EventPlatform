namespace Inventory.Tests.Domain;

// The counter-based half of no-oversell. Every seat in a general-admission section is anonymous, so
// unlike InventoryItem there is no per-seat status to check — the only thing standing between the
// platform and selling more admissions than exist is this arithmetic. Redis is the fast gate and
// Postgres holds the row, but both defer to these invariants.
public sealed class GeneralAdmissionAllocationTests
{
    [Fact]
    public void ANewAllocation_HasItsWholeCapacityRemaining()
    {
        var allocation = CreateAllocation(totalCapacity: 100);

        allocation.RemainingCapacity.ShouldBe(100);
        allocation.HeldCount.ShouldBe(0);
        allocation.SoldCount.ShouldBe(0);
    }

    [Fact]
    public void Holding_ReducesRemainingCapacityWithoutSellingAnything()
    {
        var allocation = CreateAllocation(totalCapacity: 10);

        allocation.Hold(4);

        allocation.HeldCount.ShouldBe(4);
        allocation.SoldCount.ShouldBe(0);
        allocation.RemainingCapacity.ShouldBe(6);
    }

    [Fact]
    public void HoldingExactlyTheRemainingCapacity_IsAllowed()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(7);

        allocation.Hold(3);

        allocation.RemainingCapacity.ShouldBe(0);
    }

    // The oversell case itself, at the boundary: one more than remains must not be holdable, no
    // matter how the remaining count was arrived at.
    [Fact]
    public void HoldingOneMoreThanRemains_Throws()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(6);
        allocation.MarkSold(6);
        allocation.Hold(3);

        // 10 total, 6 sold, 3 held -> 1 remaining.
        allocation.RemainingCapacity.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => allocation.Hold(2));
        allocation.RemainingCapacity.ShouldBe(1);
    }

    [Fact]
    public void SellingMovesAdmissionsFromHeldToSold_WithoutFreeingCapacity()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(4);

        allocation.MarkSold(4);

        allocation.HeldCount.ShouldBe(0);
        allocation.SoldCount.ShouldBe(4);
        allocation.RemainingCapacity.ShouldBe(6);
    }

    [Fact]
    public void ReleasingAHold_ReturnsCapacityToAvailable()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(4);

        allocation.Release(4);

        allocation.HeldCount.ShouldBe(0);
        allocation.RemainingCapacity.ShouldBe(10);
    }

    // Guards against a released hold being released twice — which would inflate remaining capacity
    // above the real total and let the section oversell.
    [Fact]
    public void ReleasingMoreThanIsHeld_Throws()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(2);

        Should.Throw<InvalidOperationException>(() => allocation.Release(3));
        allocation.RemainingCapacity.ShouldBe(8);
    }

    [Fact]
    public void SellingMoreThanIsHeld_Throws()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(2);

        Should.Throw<InvalidOperationException>(() => allocation.MarkSold(3));
        allocation.SoldCount.ShouldBe(0);
    }

    [Fact]
    public void RefundingSoldAdmissions_ReturnsThemToAvailable()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(5);
        allocation.MarkSold(5);

        allocation.ReleaseSold(2);

        allocation.SoldCount.ShouldBe(3);
        allocation.RemainingCapacity.ShouldBe(7);
    }

    [Fact]
    public void RefundingMoreThanWasSold_Throws()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        allocation.Hold(1);
        allocation.MarkSold(1);

        Should.Throw<InvalidOperationException>(() => allocation.ReleaseSold(2));
        allocation.RemainingCapacity.ShouldBe(9);
    }

    // Version drives the optimistic-concurrency check that makes Postgres, not Redis, the final
    // authority. A mutation that forgot to bump it would let two racing writers both commit.
    [Fact]
    public void EveryMutation_AdvancesTheConcurrencyToken()
    {
        var allocation = CreateAllocation(totalCapacity: 10);
        var versions = new List<int> { allocation.Version };

        allocation.Hold(3);
        versions.Add(allocation.Version);
        allocation.Release(1);
        versions.Add(allocation.Version);
        allocation.MarkSold(2);
        versions.Add(allocation.Version);
        allocation.ReleaseSold(1);
        versions.Add(allocation.Version);

        versions.ShouldBe(versions.Order().ToList());
        versions.Distinct().Count().ShouldBe(versions.Count);
    }

    [Fact]
    public void AFailedMutation_LeavesTheConcurrencyTokenAlone()
    {
        var allocation = CreateAllocation(totalCapacity: 2);
        allocation.Hold(2);
        var versionBefore = allocation.Version;

        Should.Throw<InvalidOperationException>(() => allocation.Hold(1));

        allocation.Version.ShouldBe(versionBefore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ASectionWithNoCapacity_CannotBeCreated(int totalCapacity) =>
        Should.Throw<ArgumentOutOfRangeException>(() => CreateAllocation(totalCapacity));

    private static GeneralAdmissionAllocation CreateAllocation(int totalCapacity) =>
        GeneralAdmissionAllocation.Create(
            tenantId: Guid.CreateVersion7(),
            eventSessionId: Guid.CreateVersion7(),
            catalogEventId: Guid.CreateVersion7(),
            admissionAreaId: Guid.CreateVersion7(),
            ticketTypeId: Guid.CreateVersion7(),
            priceMinor: 5_000,
            totalCapacity: totalCapacity);
}
