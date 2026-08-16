namespace Catalog.Tests.Features;

// A tour's legs are separate events clustered under one EventGroup, and three rules keep that
// cluster coherent: a leg belongs to a tour its own organizer owns, sits inside the tour's
// advertised range, and does not overlap a sibling. These are cross-aggregate checks, so they live
// in the handler rather than the validator — which means the only way to exercise them is through
// the handler. It is internal, so the command goes through MediatR exactly as the endpoint sends it,
// validation pipeline and all.
public sealed class CreateEventLegRulesTests
{
    private static readonly Guid Organizer = Guid.CreateVersion7();

    // Relative to now, not a fixed year: CreateEventValidator requires StartsAt to be in the
    // future, so pinned calendar dates would turn these green tests red the year they arrive.
    private static readonly DateTimeOffset TourStarts = DateTimeOffset.UtcNow.AddMonths(6);
    private static readonly DateTimeOffset TourEnds = TourStarts.AddDays(30);

    private readonly IEventRepository events = Substitute.For<IEventRepository>();
    private readonly IEventGroupRepository eventGroups = Substitute.For<IEventGroupRepository>();

    [Fact]
    public async Task AStandaloneEventWithNoTour_SkipsTheTourRulesEntirely()
    {
        var result = await CreateLegAsync(eventGroupId: null);

        result.Outcome.ShouldBe(CreateEventOutcome.Created);
        result.EventId.ShouldNotBeNull();
        await eventGroups.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ALegInsideItsToursRange_IsCreated()
    {
        var group = GivenTour();

        var result = await CreateLegAsync(group.Id, TourStarts.AddDays(4), TourStarts.AddDays(4).AddHours(4));

        result.Outcome.ShouldBe(CreateEventOutcome.Created);
        events.Received(1).Add(Arg.Any<Event>());
        await events.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ALegStartingBeforeItsTourDoes_IsRejected()
    {
        var group = GivenTour();

        var result = await CreateLegAsync(group.Id, TourStarts.AddDays(-1), TourStarts.AddDays(-1).AddHours(4));

        result.Outcome.ShouldBe(CreateEventOutcome.OutsideEventGroupRange);
        events.DidNotReceive().Add(Arg.Any<Event>());
    }

    [Fact]
    public async Task ALegRunningPastTheEndOfItsTour_IsRejected()
    {
        var group = GivenTour();

        var result = await CreateLegAsync(group.Id, TourEnds.AddHours(-1), TourEnds.AddHours(2));

        result.Outcome.ShouldBe(CreateEventOutcome.OutsideEventGroupRange);
    }

    // A tour created before its dates are known constrains nothing yet.
    [Fact]
    public async Task ATourWithNoDatesYet_ConstrainsNothing()
    {
        var group = GivenUndatedTour();

        var result = await CreateLegAsync(group.Id, TourStarts.AddYears(5), TourStarts.AddYears(5).AddHours(4));

        result.Outcome.ShouldBe(CreateEventOutcome.Created);
    }

    // The band cannot be in two cities at once; an overlap is a scheduling mistake, not a choice.
    [Fact]
    public async Task ALegOverlappingASiblingLeg_IsRejected()
    {
        var group = GivenTour();
        GivenExistingLegs(group.Id, (TourStarts.AddDays(4), TourStarts.AddDays(4).AddHours(6)));

        var result = await CreateLegAsync(group.Id, TourStarts.AddDays(4).AddHours(3), TourStarts.AddDays(4).AddHours(9));

        result.Outcome.ShouldBe(CreateEventOutcome.OverlapsExistingLeg);
    }

    // Overlap is checked strictly, so consecutive legs that touch at the boundary are fine — a
    // late show ending at midnight and the next city starting at midnight is a real tour schedule.
    [Fact]
    public async Task ALegStartingExactlyWhenTheLastOneEnded_IsAllowed()
    {
        var group = GivenTour();
        var previousEnds = TourStarts.AddDays(4).AddHours(6);
        GivenExistingLegs(group.Id, (TourStarts.AddDays(4), previousEnds));

        var result = await CreateLegAsync(group.Id, previousEnds, previousEnds.AddHours(4));

        result.Outcome.ShouldBe(CreateEventOutcome.Created);
    }

    [Fact]
    public async Task ALegEntirelyContainedInASiblingsRun_IsStillAnOverlap()
    {
        var group = GivenTour();
        GivenExistingLegs(group.Id, (TourStarts.AddDays(4), TourStarts.AddDays(6)));

        var result = await CreateLegAsync(group.Id, TourStarts.AddDays(5), TourStarts.AddDays(5).AddHours(2));

        result.Outcome.ShouldBe(CreateEventOutcome.OverlapsExistingLeg);
    }

    // Attaching your event to someone else's tour would put your leg on their public tour page.
    // The answer is the same opaque not-found another tenant's data always gets — never a
    // "that tour exists but isn't yours", which would confirm it exists.
    [Fact]
    public async Task ATourBelongingToAnotherOrganizer_IsNotFound()
    {
        var group = GivenTour(ownedBy: Guid.CreateVersion7());

        var result = await CreateLegAsync(group.Id);

        result.Outcome.ShouldBe(CreateEventOutcome.EventGroupNotFound);
        events.DidNotReceive().Add(Arg.Any<Event>());
    }

    [Fact]
    public async Task ATourThatDoesNotExist_IsNotFound()
    {
        eventGroups.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((EventGroup?)null);

        var result = await CreateLegAsync(Guid.CreateVersion7());

        result.Outcome.ShouldBe(CreateEventOutcome.EventGroupNotFound);
    }

    private static Event CreateLeg(Guid eventGroupId, DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        Event.Create(
            Organizer,
            "Sibling leg",
            startsAt,
            endsAt,
            "INR",
            "Venue",
            "Address line 1",
            null,
            "City",
            null,
            null,
            "IN",
            null,
            null,
            eventGroupId);

    private EventGroup GivenTour(Guid? ownedBy = null) => GivenTour(ownedBy, TourStarts, TourEnds);

    // Two named helpers rather than one with nullable date parameters: an omitted DateTimeOffset?
    // argument and an explicit null are the same value, so a defaulted call would silently produce
    // a tour with no range — which is exactly the constraint the range tests exist to exercise.
    private EventGroup GivenTour(Guid? ownedBy, DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        var group = EventGroup.Create(ownedBy ?? Organizer, "ColdPlay India Tour");
        group.Update(
            group.Title,
            startsAt,
            endsAt,
            contactPhone: null,
            contactMobile: null,
            contactEmail: null,
            websiteUrl: null,
            socialLinks: []);

        eventGroups.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        return group;
    }

    private EventGroup GivenUndatedTour() => GivenTour(ownedBy: null, startsAt: null, endsAt: null);

    private void GivenExistingLegs(Guid groupId, params (DateTimeOffset StartsAt, DateTimeOffset EndsAt)[] legs) =>
        events.ListLegsForEventGroupAsync(groupId, Arg.Any<CancellationToken>())
            .Returns(legs.Select(leg => CreateLeg(groupId, leg.StartsAt, leg.EndsAt)).ToList());

    private async Task<CreateEventResult> CreateLegAsync(
        Guid? eventGroupId,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null)
    {
        await using var provider = new ServiceCollection()
            .AddCatalogApplication()
            .AddSingleton(events)
            .AddSingleton(eventGroups)
            .BuildServiceProvider();

        var command = new CreateEventCommand(
            Organizer,
            "Mumbai",
            startsAt ?? TourStarts.AddDays(2),
            endsAt ?? TourStarts.AddDays(2).AddHours(4),
            "INR",
            "DY Patil Stadium",
            "Sector 7",
            null,
            "Navi Mumbai",
            "Maharashtra",
            "400706",
            "IN",
            null,
            null,
            eventGroupId);

        return await provider.GetRequiredService<ISender>().Send(command);
    }
}
