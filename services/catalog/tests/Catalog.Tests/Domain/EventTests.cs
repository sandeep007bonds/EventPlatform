namespace Catalog.Tests.Domain;

// Event carries the date and lifecycle rules the rest of the platform trusts without re-checking:
// Inventory enforces a booking cutoff it was handed, Ordering sells against a status it was told.
// If these guards are wrong, every service downstream is confidently wrong with them.
public sealed class EventTests
{
    private static readonly DateTimeOffset Starts = new(2027, 3, 1, 19, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ends = Starts.AddHours(4);

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

    // Draft is the only editable state. Once published, buyers may already be holding seats against
    // the dates and limits as they stand, so changing them is a different problem than editing.
    [Fact]
    public void APublishedEventsDetails_CannotBeChanged()
    {
        var @event = CreateEvent();
        @event.Publish();

        Should.Throw<InvalidOperationException>(() => UpdateDetails(@event));
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
        @event.UpdateDetails(
            description: null,
            category: null,
            endsAt: endsAt ?? Ends,
            doorsOpenAt: null,
            onSaleAt: onSaleAt,
            bookingEndsAt: bookingEndsAt,
            maxTicketsPerBuyer: null,
            requiresQueue: false,
            taxRatePercent: null,
            taxLabel: null,
            ageRestriction: null,
            bannerImageUrl: null,
            videoUrl: null,
            contactPhone: null,
            contactMobile: null,
            contactEmail: null,
            websiteUrl: null,
            socialLinks: []);
}
