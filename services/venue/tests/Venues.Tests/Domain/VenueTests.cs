namespace Venues.Tests.Domain;

// A venue is the one thing many events share. Its guards are therefore blast-radius guards: a gate
// code that is not unique, or an archived venue quietly coming back, is wrong for every event that
// points at it, not just the one being edited.
public sealed class VenueTests
{
    private static readonly VenueAddress Address = new(
        "Sector 7",
        null,
        "Navi Mumbai",
        "Maharashtra",
        "400706",
        "IN",
        19.0330,
        73.0297);

    [Fact]
    public void ANewVenue_StartsAsADraft()
    {
        var venue = CreateVenue();

        venue.Status.ShouldBe(VenueStatus.Draft);
        venue.Gates.ShouldBeEmpty();
        venue.Facilities.ShouldBeEmpty();
    }

    [Fact]
    public void AVenueRequiresAName() =>
        Should.Throw<ArgumentException>(() => Venue.Create(Guid.CreateVersion7(), "  ", null, Address, null));

    [Fact]
    public void GateCodesAreUniqueWithinAVenue()
    {
        var venue = CreateVenue();

        venue.AddGate("G3", "Gate 3 — North");

        Should.Throw<InvalidOperationException>(() => venue.AddGate("g3", "Gate 3 — South"));
    }

    [Fact]
    public void ADeactivatedGate_IsNoLongerUsableButStillExists()
    {
        var venue = CreateVenue();
        var gate = venue.AddGate("G3", "Gate 3");

        venue.HasActiveGate(gate.Id).ShouldBeTrue();

        gate.Deactivate();

        venue.HasActiveGate(gate.Id).ShouldBeFalse();
        venue.Gates.Count.ShouldBe(1);
    }

    // Archiving is one-way on purpose. "Reactivating" a venue that was retired because it was
    // demolished, renamed or sold is almost always someone reaching for the wrong venue — and the
    // events already pointing at this one keep working either way.
    [Fact]
    public void AnArchivedVenue_CannotBeReactivated()
    {
        var venue = CreateVenue();
        venue.Activate();
        venue.Archive();

        Should.Throw<InvalidOperationException>(venue.Activate);
        venue.Status.ShouldBe(VenueStatus.Archived);
    }

    [Fact]
    public void AVenuesDetails_CanBeCorrectedAtAnyStatus()
    {
        var venue = CreateVenue();
        venue.Activate();

        venue.UpdateDetails(
            "DY Patil Sports Stadium",
            "Stadium",
            Address with { PostalCode = "400614" },
            "Asia/Kolkata");

        venue.Name.ShouldBe("DY Patil Sports Stadium");
        venue.Address.PostalCode.ShouldBe("400614");
        venue.TimeZoneId.ShouldBe("Asia/Kolkata");
        venue.Status.ShouldBe(VenueStatus.Active);
    }

    private static Venue CreateVenue() =>
        Venue.Create(Guid.CreateVersion7(), "DY Patil Stadium", "Stadium", Address, "Asia/Kolkata");
}
