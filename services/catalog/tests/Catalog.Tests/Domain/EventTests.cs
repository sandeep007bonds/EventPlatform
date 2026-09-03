namespace Catalog.Tests.Domain;

// Event carries the lifecycle and selling rules the rest of the platform trusts without
// re-checking: Inventory enforces a per-buyer cap it was handed, Ordering sells against a status it
// was told. If these guards are wrong, every service downstream is confidently wrong with them.
public sealed class EventTests
{
    private static readonly DateTimeOffset Starts = new(2027, 3, 1, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ends = Starts.AddHours(4);

    [Fact]
    public void ANewEvent_StartsAsADraftWithOnePerformance()
    {
        var @event = CreateEvent();

        @event.Status.ShouldBe(EventStatus.Draft);
        @event.Sessions.Count.ShouldBe(1);
        @event.Sessions.Single().Status.ShouldBe(EventSessionStatus.Draft);
    }

    // The cached range is what the storefront sorts and filters by, so it has to track the
    // performances rather than being set once and forgotten.
    [Fact]
    public void TheEventsRange_TracksItsPerformances()
    {
        var @event = CreateEvent();

        @event.FirstSessionStartsAt.ShouldBe(Starts);
        @event.LastSessionEndsAt.ShouldBe(Ends);

        @event.AddSession("Late show", Starts.AddDays(1), Ends.AddDays(1), null, null);

        @event.FirstSessionStartsAt.ShouldBe(Starts);
        @event.LastSessionEndsAt.ShouldBe(Ends.AddDays(1));
    }

    [Fact]
    public void AnEventThatEndsBeforeItStarts_CannotBeCreated() =>
        Should.Throw<ArgumentOutOfRangeException>(() => CreateEvent(endsAt: Starts.AddHours(-1)));

    [Fact]
    public void AnEventThatEndsExactlyWhenItStarts_CannotBeCreated() =>
        Should.Throw<ArgumentOutOfRangeException>(() => CreateEvent(endsAt: Starts));

    // One act cannot be on two stages at once, whatever the calendar says.
    [Fact]
    public void TwoPerformancesOfTheSameEvent_CannotOverlap()
    {
        var @event = CreateEvent();

        Should.Throw<InvalidOperationException>(() =>
            @event.AddSession("Matinee", Starts.AddHours(1), Ends.AddHours(1), null, null));
    }

    [Fact]
    public void PerformancesThatMeetExactlyAtTheBoundary_AreAllowed()
    {
        var @event = CreateEvent();

        @event.AddSession("Late show", Ends, Ends.AddHours(3), null, null);

        @event.Sessions.Count.ShouldBe(2);
    }

    [Fact]
    public void AnEventMustKeepAtLeastOnePerformance()
    {
        var @event = CreateEvent();

        Should.Throw<InvalidOperationException>(() => @event.RemoveSession(@event.Sessions.Single().Id));
    }

    // Cancel it instead: tickets sold for a performance still have to resolve to something.
    [Fact]
    public void APerformanceThatWentOnSale_CannotBeRemoved()
    {
        var @event = CreateEvent();
        var late = @event.AddSession("Late show", Starts.AddDays(1), Ends.AddDays(1), null, null);
        MakeSellable(late);
        @event.Publish();

        Should.Throw<InvalidOperationException>(() => @event.RemoveSession(late.Id));
    }

    [Fact]
    public void PublishingWithNoSellablePerformance_IsRefused()
    {
        var @event = CreateEvent();

        Should.Throw<InvalidOperationException>(@event.Publish);
        @event.Status.ShouldBe(EventStatus.Draft);
    }

    [Fact]
    public void PublishingTakesEveryReadyPerformanceOnSale()
    {
        var @event = CreateEvent();
        MakeSellable(@event.Sessions.Single());
        var second = @event.AddSession("Night two", Starts.AddDays(1), Ends.AddDays(1), null, null);
        MakeSellable(second);

        var published = @event.Publish();

        published.Count.ShouldBe(2);
        @event.Status.ShouldBe(EventStatus.Published);
        @event.Sessions.ShouldAllBe(s => s.Status == EventSessionStatus.Published);
    }

    [Fact]
    public void AnEventCanOnlyBePublishedOnce()
    {
        var @event = PublishedEvent();

        Should.Throw<InvalidOperationException>(@event.Publish);
    }

    // The event-wide switch sweeps every performance, including a draft late show that has not
    // gone on sale — otherwise pausing a run would throw halfway through.
    [Fact]
    public void PausingTheEvent_PausesEveryPerformance()
    {
        var @event = PublishedEvent();
        @event.AddSession("Unreleased late show", Starts.AddDays(2), Ends.AddDays(2), null, null);

        @event.PauseSales();

        @event.Sessions.ShouldAllBe(s => s.SalesPaused);
        @event.AllSalesPaused().ShouldBeTrue();

        @event.ResumeSales();

        @event.Sessions.ShouldAllBe(s => !s.SalesPaused);
        @event.AllSalesPaused().ShouldBeFalse();
    }

    [Fact]
    public void SalesCanOnlyBePausedOnAPublishedEvent()
    {
        var @event = CreateEvent();

        Should.Throw<InvalidOperationException>(@event.PauseSales);
    }

    // The one rule that spans both levels: moving the on-sale later can close a night's sales
    // before they ever opened.
    [Fact]
    public void AnOnSaleTimeAfterAPerformancesBookingCutoff_IsRejected()
    {
        var @event = CreateEvent(bookingEndsAt: Starts.AddDays(-1));

        Should.Throw<ArgumentOutOfRangeException>(() => @event.UpdateSellingRules(
            onSaleAt: Starts.AddHours(-1),
            maxTicketsPerBuyer: null,
            requiresQueue: false,
            taxRatePercent: null,
            taxLabel: null,
            bookingFeePerTicketMinor: 0));
    }

    [Fact]
    public void SellingRulesCanOnlyBeChangedWhileADraft()
    {
        var @event = PublishedEvent();

        Should.Throw<InvalidOperationException>(() => @event.UpdateSellingRules(
            onSaleAt: null,
            maxTicketsPerBuyer: 4,
            requiresQueue: false,
            taxRatePercent: null,
            taxLabel: null,
            bookingFeePerTicketMinor: 0));
    }

    // The counterpart to the rule above: none of this changes what a ticket holder bought, so
    // locking it after publish only stopped organizers fixing their own mistakes.
    [Fact]
    public void APublishedEventsPresentation_CanStillBeChanged()
    {
        var @event = PublishedEvent();

        UpdatePresentation(@event, title: "Coldplay — Mumbai (rescheduled venue entrance)");

        @event.Title.ShouldBe("Coldplay — Mumbai (rescheduled venue entrance)");
        @event.Description.ShouldBe("Music of the Spheres.");
    }

    [Fact]
    public void APresentationUpdate_RequiresATitle()
    {
        var @event = CreateEvent();

        Should.Throw<ArgumentException>(() => UpdatePresentation(@event, title: "   "));
    }

    // A slug turns into a published URL the moment the event goes live. Renaming a live event is
    // fine, as the test above shows, but moving the link people were already given is not.
    [Fact]
    public void ASlug_CanBeChangedWhileDraftAndNotAfterPublish()
    {
        var @event = CreateEvent();

        @event.ChangeSlug("coldplay-mumbai-night-two");
        @event.Slug.ShouldBe("coldplay-mumbai-night-two");

        MakeSellable(@event.Sessions.Single());
        @event.Publish();

        Should.Throw<InvalidOperationException>(() => @event.ChangeSlug("coldplay-mumbai-night-three"));
    }

    [Fact]
    public void AReservedOrMalformedSlug_IsRejected()
    {
        var @event = CreateEvent();

        Should.Throw<ArgumentException>(() => @event.ChangeSlug("admin"));
        Should.Throw<ArgumentException>(() => @event.ChangeSlug("Coldplay Mumbai"));
        Should.Throw<ArgumentException>(() => @event.ChangeSlug("-leading-hyphen"));
    }

    // The visibility rule is a security boundary, not a display preference: it is what stops one
    // organizer reading another's unpublished line-up, and what keeps drafts off the public site.
    [Fact]
    public void ADraftIsVisibleOnlyToItsOwnTenant()
    {
        var owner = Guid.CreateVersion7();
        var @event = CreateEvent(tenantId: owner);

        @event.IsVisibleTo(owner).ShouldBeTrue();
        @event.IsVisibleTo(Guid.CreateVersion7()).ShouldBeFalse();
        @event.IsVisibleTo(null).ShouldBeFalse();
    }

    [Fact]
    public void OncePublished_AnEventIsVisibleToEveryoneIncludingAnonymousCallers()
    {
        var @event = PublishedEvent();

        @event.IsVisibleTo(null).ShouldBeTrue();
        @event.IsVisibleTo(Guid.CreateVersion7()).ShouldBeTrue();
    }

    private static Event CreateEvent(
        Guid? tenantId = null,
        DateTimeOffset? endsAt = null,
        DateTimeOffset? bookingEndsAt = null) =>
        Event.Create(
            tenantId ?? Guid.CreateVersion7(),
            title: "ColdPlay India Tour — Mumbai",
            slug: "coldplay-india-tour-mumbai",
            currency: "INR",
            startsAt: Starts,
            endsAt: endsAt ?? Ends,
            bookingEndsAt: bookingEndsAt);

    private static Event PublishedEvent()
    {
        var @event = CreateEvent();
        MakeSellable(@event.Sessions.Single());
        @event.Publish();

        return @event;
    }

    // A performance is only sellable once it names a seat-map version and allocates a block to a
    // ticket type. Both ids belong to other services/aggregates, so a test can invent them.
    private static void MakeSellable(EventSession session)
    {
        session.AttachSeatMap(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            new VenueSnapshot("DY Patil Stadium", "Navi Mumbai", "IN", "Asia/Kolkata"));

        session.SetAllocations([("LT", Guid.CreateVersion7())]);
    }

    private static void UpdatePresentation(Event @event, string title) =>
        @event.UpdatePresentation(
            title: title,
            description: "Music of the Spheres.",
            category: null,
            ageRestriction: null,
            bannerImageUrl: null,
            videoUrl: null,
            contactPhone: null,
            contactMobile: null,
            contactEmail: null,
            websiteUrl: null,
            socialLinks: []);
}
