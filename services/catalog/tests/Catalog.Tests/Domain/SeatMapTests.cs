namespace Catalog.Tests.Domain;

// The seat map is where a venue becomes sellable inventory: Inventory reads it once at publish time
// and provisions one row per seat and one capacity pool per general-admission section. Anything
// wrong here is baked into inventory before a single ticket is sold.
public sealed class SeatMapTests
{
    [Fact]
    public void AReservedSection_GeneratesASeatPerRowPerNumber()
    {
        var seatMap = CreateSeatMap();

        seatMap.AddReservedSection("Lower Tier", Tier("A", 7500m), rows: 4, seatsPerRow: 10);

        seatMap.Seats.Count.ShouldBe(40);
        seatMap.Capacity.ShouldBe(40);
        seatMap.Seats.Select(seat => seat.Id).Distinct().Count().ShouldBe(40);
        seatMap.Seats.ShouldAllBe(seat => seat.Section == "Lower Tier" && seat.PriceTier == "A");
    }

    [Fact]
    public void EverySeatInASection_IsUniquelyAddressableByRowAndNumber()
    {
        var seatMap = CreateSeatMap();

        seatMap.AddReservedSection("Lower Tier", Tier("A", 7500m), rows: 3, seatsPerRow: 5);

        seatMap.Seats
            .Select(seat => (seat.Row, seat.Number))
            .Distinct()
            .Count()
            .ShouldBe(15);
    }

    // A general-admission section is a counter, not seats — if it ever generated Seat rows,
    // Inventory would provision individually addressable inventory for a standing floor.
    [Fact]
    public void AGeneralAdmissionSection_GeneratesNoSeatsButStillCounts()
    {
        var seatMap = CreateSeatMap();

        seatMap.AddGeneralAdmissionSection("Floor", Tier("GA", 3500m), capacity: 500);

        seatMap.Seats.ShouldBeEmpty();
        seatMap.GeneralAdmissionSections.Count.ShouldBe(1);
        seatMap.Capacity.ShouldBe(500);
    }

    // Real venues mix the two, so capacity has to span both halves.
    [Fact]
    public void AMixedMap_CountsSeatsAndAdmissionsTogether()
    {
        var seatMap = CreateSeatMap();

        seatMap.AddReservedSection("Lower Tier", Tier("A", 7500m), rows: 4, seatsPerRow: 10);
        seatMap.AddGeneralAdmissionSection("Floor", Tier("GA", 3500m), capacity: 500);

        seatMap.Capacity.ShouldBe(540);
    }

    // Section names are how an organizer and a buyer refer to a part of the venue. Two sections
    // sharing one name makes "Floor" ambiguous everywhere downstream.
    [Fact]
    public void TwoReservedSectionsCannotShareAName()
    {
        var seatMap = CreateSeatMap();
        seatMap.AddReservedSection("Floor", Tier("A", 7500m), rows: 2, seatsPerRow: 2);

        Should.Throw<InvalidOperationException>(
            () => seatMap.AddReservedSection("Floor", Tier("B", 5000m), rows: 1, seatsPerRow: 1));
    }

    [Fact]
    public void ASectionNameIsUniqueAcrossBothKindsOfSection()
    {
        var seatMap = CreateSeatMap();
        seatMap.AddReservedSection("Floor", Tier("A", 7500m), rows: 2, seatsPerRow: 2);

        Should.Throw<InvalidOperationException>(
            () => seatMap.AddGeneralAdmissionSection("Floor", Tier("GA", 3500m), capacity: 100));
    }

    [Fact]
    public void ARejectedSection_LeavesTheMapUntouched()
    {
        var seatMap = CreateSeatMap();
        seatMap.AddReservedSection("Floor", Tier("A", 7500m), rows: 2, seatsPerRow: 2);

        Should.Throw<InvalidOperationException>(
            () => seatMap.AddReservedSection("Floor", Tier("B", 5000m), rows: 3, seatsPerRow: 3));

        seatMap.Capacity.ShouldBe(4);
    }

    // The gate is denormalised onto every seat at generation time — that is what lets Ticketing
    // answer "which gate is this ticket for?" without walking back to the section.
    [Fact]
    public void ASectionsEntryGate_IsStampedOnEverySeatItGenerates()
    {
        var seatMap = CreateSeatMap();
        var gateId = Guid.CreateVersion7();

        seatMap.AddReservedSection("North Stand", Tier("A", 7500m), rows: 2, seatsPerRow: 3, entryGateId: gateId);
        seatMap.AddGeneralAdmissionSection("Floor", Tier("GA", 3500m), capacity: 100, entryGateId: gateId);

        seatMap.Seats.ShouldAllBe(seat => seat.EntryGateId == gateId);
        seatMap.GeneralAdmissionSections.ShouldAllBe(section => section.EntryGateId == gateId);
    }

    [Fact]
    public void ASectionWithNoGate_LeavesItsSeatsUnrestricted()
    {
        var seatMap = CreateSeatMap();

        seatMap.AddReservedSection("North Stand", Tier("A", 7500m), rows: 1, seatsPerRow: 2);

        seatMap.Seats.ShouldAllBe(seat => seat.EntryGateId == null);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    [InlineData(-1, 5)]
    public void AReservedSectionWithNoSeats_IsRejected(int rows, int seatsPerRow) =>
        Should.Throw<ArgumentOutOfRangeException>(
            () => CreateSeatMap().AddReservedSection("Floor", Tier("A", 100m), rows, seatsPerRow));

    [Fact]
    public void AGeneralAdmissionSectionWithNoCapacity_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(
            () => CreateSeatMap().AddGeneralAdmissionSection("Floor", Tier("GA", 100m), capacity: 0));

    // The guard now lives on TicketType.Create rather than on the section: the type owns the price.
    [Fact]
    public void ANegativelyPricedTicketType_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(
            () => CreateSeatMap().AddReservedSection("Floor", Tier("A", -1m), rows: 1, seatsPerRow: 1));

    // Free sections are legitimate — invitations, press, accessible seating.
    [Fact]
    public void AFreeSection_IsAllowed()
    {
        var seatMap = CreateSeatMap();

        seatMap.AddReservedSection("Press", Tier("Comp", 0m), rows: 1, seatsPerRow: 10);

        seatMap.Capacity.ShouldBe(10);
    }

    private static SeatMap CreateSeatMap() =>
        SeatMap.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Stadium bowl");

    // Sections are sold as a ticket type now, not as a loose (tier name, price) pair. The tests
    // still speak in major units because that is what a seat-map request carries.
    private static TicketType Tier(string name, decimal priceAmount) =>
        TicketType.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            name,
            (long)(priceAmount * 100m));
}
