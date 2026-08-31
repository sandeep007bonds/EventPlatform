namespace Catalog.Tests.Domain;

// A ticket type is what a seat-map section is sold as: the name a buyer sees and the price they
// pay. It replaced a free-text tier string that could not be renamed and had nowhere to hang a
// sales window or a per-buyer cap. The rules worth pinning here are the ones about money and about
// what may change once an event is live.
public sealed class TicketTypeTests
{
    [Fact]
    public void ANewType_IsActive_AndKeepsItsPriceInMinorUnits()
    {
        var ticketType = Create(priceMinor: 250_000);

        ticketType.PriceMinor.ShouldBe(250_000);
        ticketType.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void AName_IsTrimmed_SoTrailingSpaceCannotCreateALookalikeType()
    {
        var ticketType = TicketType.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "  Gold  ", 1000);

        ticketType.Name.ShouldBe("Gold");
    }

    [Fact]
    public void ANegativePrice_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => Create(priceMinor: -1));

    // Free types are legitimate — press, invitations, accessible seating.
    [Fact]
    public void AFreeType_IsAllowed() => Create(priceMinor: 0).PriceMinor.ShouldBe(0);

    [Fact]
    public void ASalesWindowEndingBeforeItStarts_IsRejected()
    {
        var start = DateTimeOffset.UtcNow;

        Should.Throw<ArgumentOutOfRangeException>(() => TicketType.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Gold",
            1000,
            salesStartsAt: start,
            salesEndsAt: start.AddHours(-1)));
    }

    [Fact]
    public void AZeroPerBuyerCap_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => TicketType.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Gold",
            1000,
            maxPerBuyer: 0));

    [Fact]
    public void Renaming_IsAllowedAfterPublish_BecauseEveryReferenceIsById()
    {
        var ticketType = Create();

        ticketType.Rename("Late Release");

        ticketType.Name.ShouldBe("Late Release");
    }

    [Fact]
    public void Repricing_IsAllowedWhileTheEventIsADraft()
    {
        var ticketType = Create(priceMinor: 1000);

        ticketType.Reprice(2000, eventIsDraft: true);

        ticketType.PriceMinor.ShouldBe(2000);
    }

    // The rule that matters most here. Inventory copies the price at provisioning time, so until
    // that copy can be updated a reprice would move the storefront's number while leaving the
    // charged number alone — silently, and about money. Refusing is the safe failure.
    [Fact]
    public void Repricing_IsRefusedOnceTheEventIsPublished()
    {
        var ticketType = Create(priceMinor: 1000);

        Should.Throw<InvalidOperationException>(() => ticketType.Reprice(2000, eventIsDraft: false));

        ticketType.PriceMinor.ShouldBe(1000);
    }

    [Fact]
    public void Deactivating_RetiresTheTypeWithoutDeletingIt()
    {
        var ticketType = Create();

        ticketType.Deactivate();

        ticketType.IsActive.ShouldBeFalse();
        ticketType.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Deactivating_Twice_IsHarmless()
    {
        var ticketType = Create();

        ticketType.Deactivate();
        ticketType.Deactivate();

        ticketType.IsActive.ShouldBeFalse();
    }

    private static TicketType Create(long priceMinor = 1000) =>
        TicketType.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), "Gold", priceMinor);
}
