namespace Catalog.Domain;

/// <summary>
/// The Catalog aggregate root: a sellable event at a specific place and time. Enforces its own
/// lifecycle invariants (a draft becomes published, etc.).
/// </summary>
public sealed class Event
{
    private readonly List<EventSocialLink> _socialLinks = new();

    // Parameterless ctor for EF Core materialization.
    private Event()
    {
    }

    private Event(
        Guid id,
        Guid tenantId,
        string title,
        string slug,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string currency,
        string locationName,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string? postalCode,
        string country,
        double? latitude,
        double? longitude,
        Guid? eventGroupId,
        int? maxTicketsPerBuyer,
        bool requiresQueue,
        decimal? taxRatePercent,
        string? taxLabel,
        long bookingFeePerTicketMinor,
        string? timeZoneId)
    {
        Id = id;
        TenantId = tenantId;
        Title = title;
        Slug = slug;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Currency = currency;
        LocationName = locationName;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        EventGroupId = eventGroupId;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        RequiresQueue = requiresQueue;
        TaxRatePercent = taxRatePercent;
        TaxLabel = taxLabel;
        BookingFeePerTicketMinor = bookingFeePerTicketMinor;
        TimeZoneId = timeZoneId;
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

    /// <summary>Scheduled start time (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Scheduled end time (UTC) — this leg's run at its location ends at this time.</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Pricing currency (ISO 4217, e.g. <c>USD</c>).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Current lifecycle status.</summary>
    public EventStatus Status { get; private set; }

    /// <summary>
    /// Whether an organizer has manually paused sales for this published event (e.g. during a
    /// technical issue), independent of the <see cref="OnSaleAt"/>/<see cref="BookingEndsAt"/>
    /// enforced time window. Inventory rejects new holds while this is <see langword="true"/>,
    /// the same way it does for those bounds. See <see cref="PauseSales"/>/<see cref="ResumeSales"/>.
    /// </summary>
    public bool SalesPaused { get; private set; }

    /// <summary>Marketing description shown on the public event page.</summary>
    public string? Description { get; private set; }

    /// <summary>Free-text category (e.g. "Concert", "Comedy") — not a taxonomy table in this pass.</summary>
    public string? Category { get; private set; }

    /// <summary>Doors-open time (UTC), if different from <see cref="StartsAt"/>.</summary>
    public DateTimeOffset? DoorsOpenAt { get; private set; }

    /// <summary>
    /// Sales-window start (UTC) — before this time, Inventory rejects new holds for this event.
    /// Catalog publishes it on <see cref="Publish"/> (via <c>EventPublished</c>) so Inventory can
    /// check it at hold-placement time, the same way as <see cref="BookingEndsAt"/>.
    /// </summary>
    public DateTimeOffset? OnSaleAt { get; private set; }

    /// <summary>
    /// Booking cutoff (UTC) — after this time, Inventory rejects new holds for this event. Catalog
    /// publishes it on <see cref="Publish"/> so Inventory can check it at hold-placement time, the
    /// same way as <see cref="OnSaleAt"/>.
    /// </summary>
    public DateTimeOffset? BookingEndsAt { get; private set; }

    /// <summary>
    /// The maximum number of tickets a single buyer may hold for this event, summed across their
    /// active and converted holds. <see langword="null"/> means no limit. Enforced by Inventory at
    /// hold-placement time, propagated the same way as <see cref="BookingEndsAt"/>.
    /// </summary>
    public int? MaxTicketsPerBuyer { get; private set; }

    /// <summary>
    /// Whether a buyer must pass through the Queue service's virtual waiting room before placing
    /// a hold for this event. <see langword="false"/> (the default) means holds behave exactly as
    /// they do today — no queue detour. Propagated to Inventory and Queue via <c>EventPublished</c>;
    /// cannot change after publish in this pass, same lifecycle as <see cref="BookingEndsAt"/>.
    /// </summary>
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
    /// The venue's IANA time zone (e.g. <c>"Asia/Kolkata"</c>), or <see langword="null"/> when not
    /// set.
    /// </summary>
    /// <remarks>
    /// Every date on this aggregate is a <see cref="DateTimeOffset"/> and therefore already
    /// unambiguous as an instant — this changes nothing about when anything happens. What it fixes
    /// is display: without it a client can only render a start time in the *reader's* zone, so a
    /// buyer abroad sees a 7pm Delhi show at 1:30pm and a door time that looks wrong. Stored as an
    /// IANA identifier rather than a fixed offset because offsets change twice a year in much of
    /// the world, and an event scheduled across a DST boundary would otherwise drift.
    /// <para>
    /// Nullable, because events created before this existed have no answer and guessing one would
    /// be worse than admitting it — a client with no zone falls back to the reader's own, which is
    /// exactly today's behaviour.
    /// </para>
    /// </remarks>
    public string? TimeZoneId { get; private set; }

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

    /// <summary>Location/venue name (e.g. "Wankhede Stadium").</summary>
    public string LocationName { get; private set; } = default!;

    /// <summary>Street address, line 1.</summary>
    public string AddressLine1 { get; private set; } = default!;

    /// <summary>Street address, line 2 (suite/unit/etc.), if any.</summary>
    public string? AddressLine2 { get; private set; }

    /// <summary>City.</summary>
    public string City { get; private set; } = default!;

    /// <summary>State/province/region, if applicable.</summary>
    public string? Region { get; private set; }

    /// <summary>Postal/ZIP code, if applicable.</summary>
    public string? PostalCode { get; private set; }

    /// <summary>ISO 3166-1 alpha-2 country code (e.g. <c>US</c>).</summary>
    public string Country { get; private set; } = default!;

    /// <summary>Latitude, for a map pin — not full geocoding integration.</summary>
    public double? Latitude { get; private set; }

    /// <summary>Longitude, for a map pin.</summary>
    public double? Longitude { get; private set; }

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

    /// <summary>Creates a new draft event for the given tenant, at a specific place and time.</summary>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="title">Event title.</param>
    /// <param name="slug">URL-safe slug, unique platform-wide. See <see cref="Slug"/>.</param>
    /// <param name="startsAt">Scheduled start (UTC).</param>
    /// <param name="endsAt">Scheduled end (UTC) — must be after <paramref name="startsAt"/>.</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <param name="locationName">Location/venue name.</param>
    /// <param name="addressLine1">Street address, line 1.</param>
    /// <param name="addressLine2">Street address, line 2, if any.</param>
    /// <param name="city">City.</param>
    /// <param name="region">State/province/region, if applicable.</param>
    /// <param name="postalCode">Postal/ZIP code, if applicable.</param>
    /// <param name="country">ISO 3166-1 alpha-2 country code.</param>
    /// <param name="latitude">Latitude, if known.</param>
    /// <param name="longitude">Longitude, if known.</param>
    /// <param name="eventGroupId">
    /// The tour/series this event is one leg of, if any (see <see cref="EventGroup"/>).
    /// </param>
    /// <param name="maxTicketsPerBuyer">
    /// The maximum number of tickets a single buyer may hold for this event, if limited.
    /// See <see cref="MaxTicketsPerBuyer"/>.
    /// </param>
    /// <param name="requiresQueue">Whether to gate holds behind the Queue service's waiting room. See <see cref="RequiresQueue"/>.</param>
    /// <param name="taxRatePercent">Sales-tax rate as a percentage, if this event is taxed. See <see cref="TaxRatePercent"/>.</param>
    /// <param name="taxLabel">Display name for the tax on a receipt. See <see cref="TaxLabel"/>.</param>
    /// <param name="bookingFeePerTicketMinor">Per-ticket booking fee in minor units. See <see cref="BookingFeePerTicketMinor"/>.</param>
    /// <param name="timeZoneId">The venue's IANA time zone. See <see cref="TimeZoneId"/>.</param>
    /// <returns>A new <see cref="Event"/> in <see cref="EventStatus.Draft"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="slug"/> is malformed or reserved — see <see cref="EventSlug"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="endsAt"/> is not after <paramref name="startsAt"/>,
    /// <paramref name="taxRatePercent"/> is outside [0, 100], or
    /// <paramref name="bookingFeePerTicketMinor"/> is negative.
    /// </exception>
    public static Event Create(
        Guid tenantId,
        string title,
        string slug,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string currency,
        string locationName,
        string addressLine1,
        string? addressLine2,
        string city,
        string? region,
        string? postalCode,
        string country,
        double? latitude,
        double? longitude,
        Guid? eventGroupId,
        int? maxTicketsPerBuyer = null,
        bool requiresQueue = false,
        decimal? taxRatePercent = null,
        string? taxLabel = null,
        long bookingFeePerTicketMinor = 0,
        string? timeZoneId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (!EventSlug.IsValid(slug))
        {
            throw new ArgumentException("The slug is not a valid or permitted event slug.", nameof(slug));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(locationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        if (endsAt <= startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end time must be after the start time.");
        }

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

        return new Event(
            Guid.CreateVersion7(),
            tenantId,
            title,
            slug,
            startsAt,
            endsAt,
            currency,
            locationName,
            addressLine1,
            addressLine2,
            city,
            region,
            postalCode,
            country,
            latitude,
            longitude,
            eventGroupId,
            maxTicketsPerBuyer,
            requiresQueue,
            taxRatePercent,
            taxLabel,
            bookingFeePerTicketMinor,
            timeZoneId);
    }

    /// <summary>
    /// Publishes the event, making it sellable. Only a <see cref="EventStatus.Draft"/> may be published.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event is not a draft.</exception>
    public void Publish()
    {
        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft event can be published.");
        }

        Status = EventStatus.Published;
    }

    /// <summary>
    /// Pauses sales for a published event, without affecting already-placed holds/tickets. Only a
    /// <see cref="EventStatus.Published"/> event that isn't already paused may be paused.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event is not published, or sales are already paused.</exception>
    public void PauseSales()
    {
        if (Status != EventStatus.Published)
        {
            throw new InvalidOperationException("Only a published event's sales can be paused.");
        }

        if (SalesPaused)
        {
            throw new InvalidOperationException("Sales are already paused for this event.");
        }

        SalesPaused = true;
    }

    /// <summary>
    /// Resumes sales for a published event previously paused via <see cref="PauseSales"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event is not published, or sales are not paused.</exception>
    public void ResumeSales()
    {
        if (Status != EventStatus.Published)
        {
            throw new InvalidOperationException("Only a published event's sales can be resumed.");
        }

        if (!SalesPaused)
        {
            throw new InvalidOperationException("Sales are not paused for this event.");
        }

        SalesPaused = false;
    }

    /// <summary>
    /// Sets the things a ticket holder bought — dates, venue, tax, fees and ticketing rules. Only
    /// permitted while the event is still a <see cref="EventStatus.Draft"/>.
    /// </summary>
    /// <remarks>
    /// The draft-only rule is narrower than it used to be, and now means what it says. It applies
    /// to *material* facts: change a start time, a venue or a tax rate after publish and you have
    /// changed what someone already paid for, which needs buyer notification and possibly a refund
    /// right. Presentation — title, description, images, contact details — moved to
    /// <see cref="UpdatePresentation"/> and stays editable for the life of the event, because none
    /// of it alters the sale.
    /// <para>
    /// Postponing or relocating a published event is a real requirement and deliberately not this
    /// method. It is not an edit; it is an event of its own, with buyers to tell.
    /// </para>
    /// </remarks>
    /// <param name="startsAt">Scheduled start time (UTC).</param>
    /// <param name="endsAt">Scheduled end time (UTC) — must be after <paramref name="startsAt"/>.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if different from the start.</param>
    /// <param name="onSaleAt">Enforced sales-window start (UTC).</param>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC) — see <see cref="BookingEndsAt"/>.</param>
    /// <param name="location">Where the event happens.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit — see <see cref="MaxTicketsPerBuyer"/>.</param>
    /// <param name="requiresQueue">Whether to gate holds behind the waiting room. See <see cref="RequiresQueue"/>.</param>
    /// <param name="taxRatePercent">Sales-tax rate as a percentage — see <see cref="TaxRatePercent"/>.</param>
    /// <param name="taxLabel">Display name for the tax on a receipt — see <see cref="TaxLabel"/>.</param>
    /// <param name="bookingFeePerTicketMinor">Per-ticket booking fee in minor units.</param>
    /// <param name="timeZoneId">The venue's IANA time zone — see <see cref="TimeZoneId"/>.</param>
    /// <exception cref="InvalidOperationException">The event is not a draft.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A date, rate or fee is out of range.</exception>
    public void UpdateSchedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? onSaleAt,
        DateTimeOffset? bookingEndsAt,
        EventLocation location,
        int? maxTicketsPerBuyer,
        bool requiresQueue,
        decimal? taxRatePercent,
        string? taxLabel,
        long bookingFeePerTicketMinor,
        string? timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException(
                "An event's dates, venue and pricing rules can only be changed while it is a draft.");
        }

        if (endsAt <= startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end time must be after the start time.");
        }

        if (onSaleAt is not null && bookingEndsAt is not null && bookingEndsAt <= onSaleAt)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingEndsAt), "The booking cutoff must be after the on-sale time.");
        }

        if (bookingEndsAt is not null && bookingEndsAt > startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingEndsAt), "The booking cutoff must not be later than the event's start time.");
        }

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

        StartsAt = startsAt;
        EndsAt = endsAt;
        DoorsOpenAt = doorsOpenAt;
        OnSaleAt = onSaleAt;
        BookingEndsAt = bookingEndsAt;
        LocationName = location.Name;
        AddressLine1 = location.AddressLine1;
        AddressLine2 = location.AddressLine2;
        City = location.City;
        Region = location.Region;
        PostalCode = location.PostalCode;
        Country = location.Country;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        RequiresQueue = requiresQueue;
        TaxRatePercent = taxRatePercent;
        TaxLabel = taxLabel;
        BookingFeePerTicketMinor = bookingFeePerTicketMinor;
        TimeZoneId = timeZoneId;
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
}
