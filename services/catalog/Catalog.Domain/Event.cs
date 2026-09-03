namespace Catalog.Domain;

/// <summary>
/// The Catalog aggregate root: a sellable event, and the one or more performances it runs as.
/// </summary>
/// <remarks>
/// The event owns <b>what is being sold and how it is marketed</b> — title, page, currency, tax,
/// fees, ticket types, promo codes, policies. Each <see cref="EventSession"/> owns <b>one
/// performance</b>: its night, its venue, its seat map, its inventory.
/// <para>
/// That split is the point. Everything downstream — inventory, orders, tickets, scans, reports —
/// hangs off a session, so a three-night run is one event with three performances rather than three
/// unrelated events that happen to share a name.
/// </para>
/// </remarks>
public sealed class Event
{
    private readonly List<EventSocialLink> _socialLinks = new();
    private readonly List<EventSession> _sessions = new();

    // Parameterless ctor for EF Core materialization.
    private Event()
    {
    }

    private Event(
        Guid id,
        Guid tenantId,
        string title,
        string slug,
        string currency,
        Guid? eventGroupId,
        int? maxTicketsPerBuyer,
        bool requiresQueue,
        DateTimeOffset? onSaleAt,
        decimal? taxRatePercent,
        string? taxLabel,
        long bookingFeePerTicketMinor)
    {
        Id = id;
        TenantId = tenantId;
        Title = title;
        Slug = slug;
        Currency = currency;
        EventGroupId = eventGroupId;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        RequiresQueue = requiresQueue;
        OnSaleAt = onSaleAt;
        TaxRatePercent = taxRatePercent;
        TaxLabel = taxLabel;
        BookingFeePerTicketMinor = bookingFeePerTicketMinor;
        Status = EventStatus.Draft;
    }

    /// <summary>Unique event id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The tour/series this event is one leg of, if any. <see langword="null"/> for a standalone
    /// one-off event — the common case. See <see cref="EventGroup"/>.
    /// </summary>
    public Guid? EventGroupId { get; private set; }

    /// <summary>Event title.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// URL-safe identifier for this event, unique across the platform — the <c>/events/{slug}</c>
    /// a buyer sees instead of a GUID.
    /// </summary>
    /// <remarks>
    /// Derived from the title at creation and editable only while the event is a
    /// <see cref="EventStatus.Draft"/>. Once published the URL has been advertised, and a slug that
    /// moves is a link that breaks — including links this platform did not issue. Renaming a
    /// published event therefore changes <see cref="Title"/> and leaves this alone, which is a
    /// little untidy and much better than a dead poster.
    /// <para>
    /// Uniqueness is enforced by a unique index, not here: an aggregate cannot see its siblings.
    /// </para>
    /// </remarks>
    public string Slug { get; private set; } = default!;

    /// <summary>Pricing currency (ISO 4217, e.g. <c>USD</c>).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Current lifecycle status.</summary>
    public EventStatus Status { get; private set; }

    /// <summary>The performances this event runs as. Always at least one.</summary>
    public IReadOnlyCollection<EventSession> Sessions => _sessions;

    /// <summary>
    /// When the first performance starts, denormalised from <see cref="Sessions"/>.
    /// </summary>
    /// <remarks>
    /// A stored column rather than a computed property, and maintained by this aggregate whenever a
    /// session is added, moved or removed. The storefront lists events ordered by date and filtered
    /// to what is still upcoming; computing this in memory would turn that indexed scan into
    /// loading every session of every event. Never set from outside — see <see cref="RefreshRange"/>.
    /// </remarks>
    public DateTimeOffset? FirstSessionStartsAt { get; private set; }

    /// <summary>When the last performance ends, denormalised from <see cref="Sessions"/>.</summary>
    public DateTimeOffset? LastSessionEndsAt { get; private set; }

    /// <summary>Marketing description shown on the public event page.</summary>
    public string? Description { get; private set; }

    /// <summary>Free-text category (e.g. "Concert", "Comedy") — not a taxonomy table in this pass.</summary>
    public string? Category { get; private set; }

    /// <summary>
    /// Sales-window start (UTC) — before this time, Inventory rejects new holds for any of this
    /// event's performances.
    /// </summary>
    /// <remarks>
    /// On the <b>event</b>, not the session: a run goes on sale once, at one advertised moment, for
    /// every night at the same time. The booking <i>cutoff</i> is the opposite and lives on the
    /// session, because "book until two hours before the show" is a different instant every night.
    /// </remarks>
    public DateTimeOffset? OnSaleAt { get; private set; }

    /// <summary>
    /// The maximum number of tickets a single buyer may hold for this event, summed across their
    /// active and converted holds. <see langword="null"/> means no limit.
    /// </summary>
    /// <remarks>
    /// Per event rather than per performance, and deliberately: a limit that reset every night
    /// would let one buyer take the cap three times over on a three-night run, which is exactly the
    /// behaviour it exists to prevent. Inventory keeps the event id alongside the session id so it
    /// can count across the whole run.
    /// </remarks>
    public int? MaxTicketsPerBuyer { get; private set; }

    /// <summary>
    /// Whether a buyer must pass through the Queue service's virtual waiting room before placing a
    /// hold.
    /// </summary>
    /// <remarks>
    /// On the event, like <see cref="OnSaleAt"/> and for the same reason: the waiting room gates the
    /// on-sale, and an on-sale covers the whole run. One admission token admits a buyer to the
    /// event, not to one night of it.
    /// </remarks>
    public bool RequiresQueue { get; private set; }

    /// <summary>
    /// Sales-tax rate applied to this event's orders, as a percentage (e.g. <c>18</c> for 18% GST).
    /// <see langword="null"/> or zero means no tax. Ordering reads it at checkout and applies it to
    /// the **post-discount** subtotal — Catalog stores the rate and computes nothing.
    /// </summary>
    public decimal? TaxRatePercent { get; private set; }

    /// <summary>
    /// What to call the tax on a receipt (e.g. <c>"GST 18%"</c>, <c>"VAT"</c>). Display only —
    /// the arithmetic uses <see cref="TaxRatePercent"/> alone.
    /// </summary>
    public string? TaxLabel { get; private set; }

    /// <summary>
    /// Booking fee charged per ticket, in minor currency units (e.g. <c>3000</c> for ₹30 a ticket).
    /// Zero means no fee.
    /// </summary>
    /// <remarks>
    /// Per ticket rather than per order, so it scales with what the buyer actually gets, and stored
    /// in minor units rather than as a percentage so an organizer advertising "₹30 booking fee" can
    /// state the exact number. As with <see cref="TaxRatePercent"/>, Catalog stores it and computes
    /// nothing — Ordering owns the money (ADR-0034).
    /// </remarks>
    public long BookingFeePerTicketMinor { get; private set; }

    /// <summary>Free-text age restriction (e.g. "18+", "All ages"), if any.</summary>
    public string? AgeRestriction { get; private set; }

    /// <summary>
    /// URL of the banner image shown on the public event page. Set by pasting the URL returned
    /// from the Media service's upload endpoint — this service has no awareness of blob storage.
    /// </summary>
    public string? BannerImageUrl { get; private set; }

    /// <summary>Video embed URL (e.g. YouTube/Vimeo link) — not a hosted/uploaded video file.</summary>
    public string? VideoUrl { get; private set; }

    /// <summary>Contact phone for this leg. <see langword="null"/> falls back to the owning <see cref="EventGroup"/>'s default.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Contact mobile number for this leg — see <see cref="ContactPhone"/>.</summary>
    public string? ContactMobile { get; private set; }

    /// <summary>Contact email for this leg — see <see cref="ContactPhone"/>.</summary>
    public string? ContactEmail { get; private set; }

    /// <summary>Website URL for this leg — see <see cref="ContactPhone"/>.</summary>
    public string? WebsiteUrl { get; private set; }

    /// <summary>
    /// This leg's own social links. When non-empty, these entirely replace (not merge with) the
    /// owning <see cref="EventGroup"/>'s default social links.
    /// </summary>
    public IReadOnlyCollection<EventSocialLink> SocialLinks => _socialLinks;

    /// <summary>Creates a new draft event with its first performance.</summary>
    /// <remarks>
    /// An event is created <b>with</b> a performance rather than gaining one later, because an event
    /// with no session sells nothing, has no date to list it by, and cannot be checked against its
    /// tour's range. The single-performance case — the overwhelming majority — then needs no extra
    /// step at all.
    /// </remarks>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="title">Event title.</param>
    /// <param name="slug">URL-safe slug, unique platform-wide. See <see cref="Slug"/>.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="startsAt">The first performance's start (UTC).</param>
    /// <param name="endsAt">The first performance's end (UTC) — must be after <paramref name="startsAt"/>.</param>
    /// <param name="doorsOpenAt">The first performance's doors-open time (UTC), if different.</param>
    /// <param name="bookingEndsAt">The first performance's booking cutoff (UTC), if any.</param>
    /// <param name="eventGroupId">The tour/series this event is one leg of, if any.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit — see <see cref="MaxTicketsPerBuyer"/>.</param>
    /// <param name="requiresQueue">Whether to gate holds behind the waiting room. See <see cref="RequiresQueue"/>.</param>
    /// <param name="onSaleAt">Enforced sales-window start (UTC) — see <see cref="OnSaleAt"/>.</param>
    /// <param name="taxRatePercent">Sales-tax rate as a percentage, if this event is taxed.</param>
    /// <param name="taxLabel">Display name for the tax on a receipt.</param>
    /// <param name="bookingFeePerTicketMinor">Per-ticket booking fee in minor units.</param>
    /// <returns>A new <see cref="Event"/> in <see cref="EventStatus.Draft"/>, with one draft session.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="slug"/> is malformed or reserved — see <see cref="EventSlug"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">A date, rate or fee is out of range.</exception>
    public static Event Create(
        Guid tenantId,
        string title,
        string slug,
        string currency,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt = null,
        DateTimeOffset? bookingEndsAt = null,
        Guid? eventGroupId = null,
        int? maxTicketsPerBuyer = null,
        bool requiresQueue = false,
        DateTimeOffset? onSaleAt = null,
        decimal? taxRatePercent = null,
        string? taxLabel = null,
        long bookingFeePerTicketMinor = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (!EventSlug.IsValid(slug))
        {
            throw new ArgumentException("The slug is not a valid or permitted event slug.", nameof(slug));
        }

        ValidateCommercials(taxRatePercent, bookingFeePerTicketMinor, onSaleAt, bookingEndsAt);

        var @event = new Event(
            Guid.CreateVersion7(),
            tenantId,
            title,
            slug,
            currency,
            eventGroupId,
            maxTicketsPerBuyer,
            requiresQueue,
            onSaleAt,
            taxRatePercent,
            taxLabel,
            bookingFeePerTicketMinor);

        @event.AddSession(null, startsAt, endsAt, doorsOpenAt, bookingEndsAt);

        return @event;
    }

    /// <summary>Adds a performance.</summary>
    /// <remarks>
    /// Allowed after publish. Adding a late show to a run that is already selling is ordinary work,
    /// and it is additive — the new performance is a draft until its own seat map and pricing are
    /// set, so nothing about the event's existing sales changes when it appears.
    /// </remarks>
    /// <param name="name">What to call it when there is more than one, e.g. <c>Matinee</c>.</param>
    /// <param name="startsAt">Scheduled start (UTC).</param>
    /// <param name="endsAt">Scheduled end (UTC) — must be after <paramref name="startsAt"/>.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if different from the start.</param>
    /// <param name="bookingEndsAt">Booking cutoff (UTC), if any.</param>
    /// <returns>The new performance.</returns>
    /// <exception cref="InvalidOperationException">It overlaps a performance this event already has.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A date is out of range.</exception>
    public EventSession AddSession(
        string? name,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? bookingEndsAt)
    {
        EventSession.ValidateTimes(startsAt, endsAt, doorsOpenAt, bookingEndsAt);
        EnsureNoOverlap(startsAt, endsAt, exceptSessionId: null);
        EnsureCutoffAfterOnSale(bookingEndsAt);

        var session = new EventSession(
            Guid.CreateVersion7(),
            Id,
            TenantId,
            string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            startsAt,
            endsAt,
            doorsOpenAt,
            bookingEndsAt);

        _sessions.Add(session);
        RefreshRange();

        return session;
    }

    /// <summary>Moves a performance in time, keeping the event's cached date range correct.</summary>
    /// <param name="sessionId">The performance to move.</param>
    /// <param name="startsAt">Scheduled start (UTC).</param>
    /// <param name="endsAt">Scheduled end (UTC).</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if different from the start.</param>
    /// <param name="bookingEndsAt">Booking cutoff (UTC), if any.</param>
    /// <exception cref="InvalidOperationException">
    /// No such performance, it is not a draft, or the new times overlap another performance.
    /// </exception>
    public void RescheduleSession(
        Guid sessionId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? bookingEndsAt)
    {
        var session = RequireSession(sessionId);

        EventSession.ValidateTimes(startsAt, endsAt, doorsOpenAt, bookingEndsAt);
        EnsureNoOverlap(startsAt, endsAt, exceptSessionId: sessionId);
        EnsureCutoffAfterOnSale(bookingEndsAt);

        session.Reschedule(startsAt, endsAt, doorsOpenAt, bookingEndsAt);
        RefreshRange();
    }

    /// <summary>
    /// Removes a performance that never went on sale. The last one cannot be removed — an event with
    /// no performance sells nothing and has no date.
    /// </summary>
    /// <param name="sessionId">The performance to remove.</param>
    /// <exception cref="InvalidOperationException">
    /// No such performance, it has been published (cancel it instead), or it is the only one.
    /// </exception>
    public void RemoveSession(Guid sessionId)
    {
        var session = RequireSession(sessionId);

        if (session.Status != EventSessionStatus.Draft)
        {
            throw new InvalidOperationException(
                "A performance that has been on sale cannot be removed. Cancel it instead, so tickets sold for it still resolve.");
        }

        if (_sessions.Count == 1)
        {
            throw new InvalidOperationException("An event must keep at least one performance.");
        }

        _sessions.Remove(session);
        RefreshRange();
    }

    /// <summary>Gets one of this event's performances, or <see langword="null"/>.</summary>
    /// <param name="sessionId">The performance id.</param>
    /// <returns>The performance, or <see langword="null"/> if this event has no such one.</returns>
    public EventSession? FindSession(Guid sessionId) => _sessions.FirstOrDefault(s => s.Id == sessionId);

    /// <summary>
    /// Publishes the event and every performance that is ready, making it sellable.
    /// </summary>
    /// <remarks>
    /// Returns the sessions that went live so the caller can announce one integration event each —
    /// inventory is provisioned per performance, so one message per event would not say enough.
    /// </remarks>
    /// <returns>The performances that were published by this call.</returns>
    /// <exception cref="InvalidOperationException">
    /// The event is not a draft, or no performance is ready to sell.
    /// </exception>
    public IReadOnlyList<EventSession> Publish()
    {
        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft event can be published.");
        }

        var ready = _sessions
            .Where(s => s.Status == EventSessionStatus.Draft && s.IsSellable)
            .ToList();

        if (ready.Count == 0)
        {
            throw new InvalidOperationException(
                "No performance is ready to sell. Each one needs a published seat map and at least one allocated block.");
        }

        foreach (var session in ready)
        {
            session.Publish();
        }

        Status = EventStatus.Published;

        return ready;
    }

    /// <summary>
    /// Pauses sales across every performance, without affecting already-placed holds or tickets.
    /// </summary>
    /// <remarks>
    /// The event-wide switch. Pulling a single night is
    /// <see cref="EventSession.PauseSales"/> on that session instead.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The event is not published.</exception>
    public void PauseSales()
    {
        EnsurePublished();

        foreach (var session in _sessions)
        {
            session.SetSalesPaused(true);
        }
    }

    /// <summary>Resumes sales across every performance.</summary>
    /// <exception cref="InvalidOperationException">The event is not published.</exception>
    public void ResumeSales()
    {
        EnsurePublished();

        foreach (var session in _sessions)
        {
            session.SetSalesPaused(false);
        }
    }

    /// <summary>Whether sales are paused on every one of this event's performances.</summary>
    /// <returns><see langword="true"/> when nothing is currently selling.</returns>
    public bool AllSalesPaused() => _sessions.Count > 0 && _sessions.TrueForAll(s => s.SalesPaused);

    /// <summary>
    /// Sets the commercial terms a ticket holder bought under — currency rules, tax, fees, the
    /// on-sale time and the per-buyer limit. Only while the event is still a
    /// <see cref="EventStatus.Draft"/>.
    /// </summary>
    /// <remarks>
    /// Much smaller than it used to be: the dates and the venue moved to
    /// <see cref="EventSession"/>, where they belong, and are edited per performance. What is left
    /// here is the money and the selling rules, which really are one decision for the whole run.
    /// </remarks>
    /// <param name="onSaleAt">Enforced sales-window start (UTC) — see <see cref="OnSaleAt"/>.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit — see <see cref="MaxTicketsPerBuyer"/>.</param>
    /// <param name="requiresQueue">Whether to gate holds behind the waiting room.</param>
    /// <param name="taxRatePercent">Sales-tax rate as a percentage — see <see cref="TaxRatePercent"/>.</param>
    /// <param name="taxLabel">Display name for the tax on a receipt.</param>
    /// <param name="bookingFeePerTicketMinor">Per-ticket booking fee in minor units.</param>
    /// <exception cref="InvalidOperationException">The event is not a draft.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A rate, fee or date is out of range.</exception>
    public void UpdateSellingRules(
        DateTimeOffset? onSaleAt,
        int? maxTicketsPerBuyer,
        bool requiresQueue,
        decimal? taxRatePercent,
        string? taxLabel,
        long bookingFeePerTicketMinor)
    {
        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException(
                "An event's selling rules can only be changed while it is a draft.");
        }

        // Min over a nullable selector yields null for an empty sequence, which is exactly the
        // "no performance has a cutoff" case.
        var earliestCutoff = _sessions.Min(s => s.BookingEndsAt);

        ValidateCommercials(taxRatePercent, bookingFeePerTicketMinor, onSaleAt, earliestCutoff);

        OnSaleAt = onSaleAt;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        RequiresQueue = requiresQueue;
        TaxRatePercent = taxRatePercent;
        TaxLabel = taxLabel;
        BookingFeePerTicketMinor = bookingFeePerTicketMinor;
    }

    /// <summary>
    /// Sets how the event is presented — title, description, imagery, contact and social links.
    /// Permitted at <b>any</b> status.
    /// </summary>
    /// <remarks>
    /// None of this changes what a ticket holder bought, so locking it after publish only stopped
    /// organizers fixing their own mistakes. <see cref="Title"/> is included deliberately: renaming
    /// a live event is mildly disruptive, and being permanently unable to correct a typo in it is
    /// worse. Who changed what is recorded by the audit fields (ADR-0036).
    /// </remarks>
    /// <param name="title">Event title.</param>
    /// <param name="description">Marketing description.</param>
    /// <param name="category">Free-text category.</param>
    /// <param name="ageRestriction">Free-text age restriction.</param>
    /// <param name="bannerImageUrl">Banner image URL (from the Media service's upload endpoint).</param>
    /// <param name="videoUrl">Video embed URL.</param>
    /// <param name="contactPhone">Contact phone for this leg, overriding the tour default.</param>
    /// <param name="contactMobile">Contact mobile number for this leg, overriding the tour default.</param>
    /// <param name="contactEmail">Contact email for this leg, overriding the tour default.</param>
    /// <param name="websiteUrl">Website URL for this leg, overriding the tour default.</param>
    /// <param name="socialLinks">
    /// This leg's own social links (platform, URL pairs); replaces the existing list. An empty
    /// list means "no override" — the tour's default social links apply instead.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="title"/> is null or blank.</exception>
    public void UpdatePresentation(
        string title,
        string? description,
        string? category,
        string? ageRestriction,
        string? bannerImageUrl,
        string? videoUrl,
        string? contactPhone,
        string? contactMobile,
        string? contactEmail,
        string? websiteUrl,
        IEnumerable<(string Platform, string Url)> socialLinks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(socialLinks);

        Title = title.Trim();
        Description = description;
        Category = category;
        AgeRestriction = ageRestriction;
        BannerImageUrl = bannerImageUrl;
        VideoUrl = videoUrl;
        ContactPhone = contactPhone;
        ContactMobile = contactMobile;
        ContactEmail = contactEmail;
        WebsiteUrl = websiteUrl;

        _socialLinks.Clear();
        _socialLinks.AddRange(socialLinks.Select(link => new EventSocialLink(Guid.CreateVersion7(), Id, link.Platform, link.Url)));
    }

    /// <summary>
    /// Changes the event's public slug. Only permitted while the event is a
    /// <see cref="EventStatus.Draft"/> — see <see cref="Slug"/> for why.
    /// </summary>
    /// <param name="slug">The new slug; must be well-formed and not reserved.</param>
    /// <exception cref="InvalidOperationException">The event is not a draft.</exception>
    /// <exception cref="ArgumentException">The slug is malformed or reserved.</exception>
    public void ChangeSlug(string slug)
    {
        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException(
                "An event's URL can only be changed while it is a draft — it has already been advertised.");
        }

        if (!EventSlug.IsValid(slug))
        {
            throw new ArgumentException("The slug is not a valid or permitted event slug.", nameof(slug));
        }

        Slug = slug;
    }

    /// <summary>
    /// Whether this event is visible to the given caller: everyone can see a
    /// <see cref="EventStatus.Published"/> event (and beyond); only the owning tenant can see a
    /// <see cref="EventStatus.Draft"/> event, never an anonymous caller or another tenant.
    /// </summary>
    /// <param name="callerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
    /// <returns><see langword="true"/> if the caller may see this event.</returns>
    public bool IsVisibleTo(Guid? callerTenantId) =>
        Status != EventStatus.Draft || (callerTenantId is not null && callerTenantId == TenantId);

    private static void ValidateCommercials(
        decimal? taxRatePercent,
        long bookingFeePerTicketMinor,
        DateTimeOffset? onSaleAt,
        DateTimeOffset? earliestBookingEndsAt)
    {
        if (taxRatePercent is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxRatePercent),
                taxRatePercent,
                "The tax rate must be between 0 and 100 percent.");
        }

        if (bookingFeePerTicketMinor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookingFeePerTicketMinor),
                bookingFeePerTicketMinor,
                "The booking fee cannot be negative.");
        }

        if (onSaleAt is not null && earliestBookingEndsAt is not null && earliestBookingEndsAt <= onSaleAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(onSaleAt),
                "Sales would close before they opened: a performance's booking cutoff is at or before the on-sale time.");
        }
    }

    private void RefreshRange()
    {
        FirstSessionStartsAt = _sessions.Count == 0 ? null : _sessions.Min(s => s.StartsAt);
        LastSessionEndsAt = _sessions.Count == 0 ? null : _sessions.Max(s => s.EndsAt);
    }

    private void EnsureNoOverlap(DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? exceptSessionId)
    {
        var clash = _sessions.Any(s => s.Id != exceptSessionId && s.Overlaps(startsAt, endsAt));

        if (clash)
        {
            throw new InvalidOperationException(
                "This event already has a performance running at that time. Two performances of the same event cannot overlap.");
        }
    }

    private void EnsureCutoffAfterOnSale(DateTimeOffset? bookingEndsAt)
    {
        if (OnSaleAt is not null && bookingEndsAt is not null && bookingEndsAt <= OnSaleAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookingEndsAt),
                "The booking cutoff must be after the event goes on sale.");
        }
    }

    private void EnsurePublished()
    {
        if (Status != EventStatus.Published)
        {
            throw new InvalidOperationException("Only a published event's sales can be paused or resumed.");
        }
    }

    private EventSession RequireSession(Guid sessionId) =>
        FindSession(sessionId)
        ?? throw new InvalidOperationException("This event has no such performance.");
}
