namespace Catalog.Tests.Domain;

// Event carries the date and lifecycle rules the rest of the platform trusts without re-checking:
// Inventory enforces a booking cutoff it was handed, Ordering sells against a status it was told.
// If these guards are wrong, every service downstream is confidently wrong with them.
public sealed class EventTests
{
    private static readonly DateTimeOffset Starts = new(2027, 3, 1, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ends = Starts.AddHours(4);

    private static readonly EventLocation Location = new(
        "DY Patil Stadium",
        "Sector 7",
        null,
        "Navi Mumbai",
        "Maharashtra",
        "400706",
        "IN",
        null,
        null);

    [Fact]
    public void ANewEvent_StartsAsADraft()
    {
        var @event = CreateEvent();

        @event.Status.ShouldBe(EventStatus.Draft);
        @event.SalesPaused.ShouldBeFalse();
    }

    [Fact]
    public void AnEventThatEndsBeforeItStarts_CannotBeCreated() =>
        Should.Throw<ArgumentOutOfRangeException>(() => CreateEvent(endsAt: Starts.AddHours(-1)));

    [Fact]
    public void AnEventThatEndsExactlyWhenItStarts_CannotBeCreated() =>
        Should.Throw<ArgumentOutOfRangeException>(() => CreateEvent(endsAt: Starts));

    // Draft is the only editable state for the *schedule*. Once published, buyers may already be
    // holding seats against the dates and limits as they stand, so changing them is a different
    // problem than editing.
    [Fact]
    public void APublishedEventsSchedule_CannotBeChanged()
    {
        var @event = CreateEvent();
        @event.Publish();

        Should.Throw<InvalidOperationException>(() => UpdateDetails(@event));
    }

    // The counterpart to the rule above, and the reason the two were split: none of this changes
    // what a ticket holder bought, so locking it after publish only stopped organizers fixing their
    // own mistakes.
    [Fact]
    public void APublishedEventsPresentation_CanStillBeChanged()
    {
        var @event = CreateEvent();
        @event.Publish();

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

    // The slug is a published URL as soon as the event is. Renaming a live event is allowed above;
    // moving the link people were given is not.
    [Fact]
    public void ASlug_CanBeChangedWhileDraftAndNotAfterPublish()
    {
        var @event = CreateEvent();

        @event.ChangeSlug("coldplay-mumbai-night-two");
        @event.Slug.ShouldBe("coldplay-mumbai-night-two");

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

    [Fact]
    public void AnEventCanOnlyBePublishedOnce()
    {
        var @event = CreateEvent();
        @event.Publish();

        Should.Throw<InvalidOperationException>(@event.Publish);
    }

    [Fact]
    public void UpdatingDetails_CanMoveTheEndTimeButNotBeforeTheStart()
    {
        var @event = CreateEvent();

        UpdateDetails(@event, endsAt: Starts.AddHours(6));
        @event.EndsAt.ShouldBe(Starts.AddHours(6));

        Should.Throw<ArgumentOutOfRangeException>(() => UpdateDetails(@event, endsAt: Starts.AddMinutes(-1)));
    }

    [Fact]
    public void ABookingCutoffBeforeTheOnSaleTime_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => UpdateDetails(
            CreateEvent(),
            onSaleAt: Starts.AddDays(-10),
            bookingEndsAt: Starts.AddDays(-20)));

    // Selling a ticket after the doors have opened is a different feature (walk-ups), not something
    // the cutoff should quietly allow by being set past the start time.
    [Fact]
    public void ABookingCutoffAfterTheEventStarts_IsRejected() =>
        Should.Throw<ArgumentOutOfRangeException>(() => UpdateDetails(
            CreateEvent(),
            bookingEndsAt: Starts.AddMinutes(1)));

    [Fact]
    public void ABookingCutoffExactlyAtTheStartTime_IsAllowed()
    {
        var @event = CreateEvent();

        UpdateDetails(@event, bookingEndsAt: Starts);

        @event.BookingEndsAt.ShouldBe(Starts);
    }

    [Fact]
    public void SalesCanOnlyBePausedOnAPublishedEvent()
    {
        var @event = CreateEvent();

        Should.Throw<InvalidOperationException>(@event.PauseSales);

        @event.Publish();
        @event.PauseSales();
        @event.SalesPaused.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(@event.PauseSales);
    }

    [Fact]
    public void ResumingSalesRequiresThemToBePaused()
    {
        var @event = CreateEvent();
        @event.Publish();

        Should.Throw<InvalidOperationException>(@event.ResumeSales);

        @event.PauseSales();
        @event.ResumeSales();
        @event.SalesPaused.ShouldBeFalse();
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
        var @event = CreateEvent();
        @event.Publish();

        @event.IsVisibleTo(null).ShouldBeTrue();
        @event.IsVisibleTo(Guid.CreateVersion7()).ShouldBeTrue();
    }

    private static Event CreateEvent(Guid? tenantId = null, DateTimeOffset? endsAt = null) =>
        Event.Create(
            tenantId ?? Guid.CreateVersion7(),
            title: "ColdPlay India Tour — Mumbai",
            slug: "coldplay-india-tour-mumbai",
            startsAt: Starts,
            endsAt: endsAt ?? Ends,
            currency: "INR",
            locationName: "DY Patil Stadium",
            addressLine1: "Sector 7",
            addressLine2: null,
            city: "Navi Mumbai",
            region: "Maharashtra",
            postalCode: "400706",
            country: "IN",
            latitude: null,
            longitude: null,
            eventGroupId: null);

    private static void UpdateDetails(
        Event @event,
        DateTimeOffset? endsAt = null,
        DateTimeOffset? onSaleAt = null,
        DateTimeOffset? bookingEndsAt = null) =>
        @event.UpdateSchedule(
            startsAt: Starts,
            endsAt: endsAt ?? Ends,
            doorsOpenAt: null,
            onSaleAt: onSaleAt,
            bookingEndsAt: bookingEndsAt,
            location: Location,
            maxTicketsPerBuyer: null,
            requiresQueue: false,
            taxRatePercent: null,
            taxLabel: null,
            bookingFeePerTicketMinor: 0,
            timeZoneId: null);

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
