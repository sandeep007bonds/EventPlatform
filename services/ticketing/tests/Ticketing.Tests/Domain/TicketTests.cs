namespace Ticketing.Tests.Domain;

// A ticket is the last thing standing between a paid order and someone walking through a gate.
// Its whole job is to be admitted exactly once.
public sealed class TicketTests
{
    [Fact]
    public void ANewTicket_IsIssuedAndNotYetCheckedIn()
    {
        var ticket = CreateSeatTicket();

        ticket.Status.ShouldBe(TicketStatus.Issued);
        ticket.CheckedInAt.ShouldBeNull();
    }

    // Seat and general admission are the two shapes a ticket can take, and they are exclusive: a
    // ticket admitting both would be ambiguous at the gate and double-counted in reporting.
    [Fact]
    public void ATicketMustAdmitEitherASeatOrAnAllocation_NeverBoth() =>
        Should.Throw<ArgumentException>(() => Ticket.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            seatId: Guid.CreateVersion7(),
            generalAdmissionAllocationId: Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "TOKEN"));

    [Fact]
    public void ATicketAdmittingNeither_IsRejected() =>
        Should.Throw<ArgumentException>(() => Ticket.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            seatId: null,
            generalAdmissionAllocationId: null,
            Guid.CreateVersion7(),
            "TOKEN"));

    [Fact]
    public void AGeneralAdmissionTicket_CarriesNoSeat()
    {
        var allocationId = Guid.CreateVersion7();

        var ticket = CreateGeneralAdmissionTicket(allocationId);

        ticket.SeatId.ShouldBeNull();
        ticket.GeneralAdmissionAllocationId.ShouldBe(allocationId);
    }

    [Fact]
    public void CheckingIn_RecordsWhenItHappened()
    {
        var ticket = CreateSeatTicket();
        var before = DateTimeOffset.UtcNow;

        ticket.CheckIn();

        ticket.Status.ShouldBe(TicketStatus.CheckedIn);
        ticket.CheckedInAt.ShouldNotBeNull();
        ticket.CheckedInAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    // The one that matters at the gate: a screenshot of someone else's ticket, or the same phone
    // presented twice, must not admit a second person.
    [Fact]
    public void ATicketCannotBeCheckedInTwice()
    {
        var ticket = CreateSeatTicket();
        ticket.CheckIn();
        var firstEntry = ticket.CheckedInAt;

        Should.Throw<InvalidOperationException>(ticket.CheckIn);

        ticket.CheckedInAt.ShouldBe(firstEntry);
    }

    [Fact]
    public void AVoidedTicket_CannotBeCheckedIn()
    {
        var ticket = CreateSeatTicket();
        ticket.Void();

        Should.Throw<InvalidOperationException>(ticket.CheckIn);
        ticket.CheckedInAt.ShouldBeNull();
    }

    // Voiding follows a refund, which can be retried — so it has to be idempotent rather than
    // throwing on a second call the way check-in does.
    [Fact]
    public void VoidingIsIdempotent()
    {
        var ticket = CreateSeatTicket();

        ticket.Void();
        ticket.Void();

        ticket.Status.ShouldBe(TicketStatus.Void);
    }

    // A refund after someone has already walked in is a real situation; the audit trail of when
    // they entered must survive it.
    [Fact]
    public void VoidingACheckedInTicket_KeepsTheRecordOfEntry()
    {
        var ticket = CreateSeatTicket();
        ticket.CheckIn();
        var entry = ticket.CheckedInAt;

        ticket.Void();

        ticket.Status.ShouldBe(TicketStatus.Void);
        ticket.CheckedInAt.ShouldBe(entry);
    }

    [Fact]
    public void ATicketWithNoToken_IsRejected() =>
        Should.Throw<ArgumentException>(() => Ticket.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            seatId: Guid.CreateVersion7(),
            generalAdmissionAllocationId: null,
            Guid.CreateVersion7(),
            "  "));

    private static Ticket CreateSeatTicket() => Ticket.Create(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        seatId: Guid.CreateVersion7(),
        generalAdmissionAllocationId: null,
        Guid.CreateVersion7(),
        "TOKEN");

    private static Ticket CreateGeneralAdmissionTicket(Guid allocationId) => Ticket.Create(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        seatId: null,
        generalAdmissionAllocationId: allocationId,
        Guid.CreateVersion7(),
        "TOKEN");
}
