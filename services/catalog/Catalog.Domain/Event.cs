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
        int? maxTicketsPerBuyer)
    {
        Id = id;
        TenantId = tenantId;
        Title = title;
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

    /// <summary>Scheduled start time (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Scheduled end time (UTC) — this leg's run at its location ends at this time.</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Pricing currency (ISO 4217, e.g. <c>USD</c>).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Current lifecycle status.</summary>
    public EventStatus Status { get; private set; }

    /// <summary>Marketing description shown on the public event page.</summary>
    public string? Description { get; private set; }

    /// <summary>Free-text category (e.g. "Concert", "Comedy") — not a taxonomy table in this pass.</summary>
    public string? Category { get; private set; }

    /// <summary>Doors-open time (UTC), if different from <see cref="StartsAt"/>.</summary>
    public DateTimeOffset? DoorsOpenAt { get; private set; }

    /// <summary>
    /// Display-only sales-window start (UTC). Not enforced by <see cref="Publish"/> or any status
    /// transition — purely informational on the public event page. Contrast with
    /// <see cref="BookingEndsAt"/>, which is enforced.
    /// </summary>
    public DateTimeOffset? OnSaleAt { get; private set; }

    /// <summary>
    /// Booking cutoff (UTC) — after this time, Inventory rejects new holds for this event. This is
    /// a real, enforced invariant (unlike <see cref="OnSaleAt"/>): Catalog publishes it on
    /// <see cref="Publish"/> so Inventory can check it at hold-placement time.
    /// </summary>
    public DateTimeOffset? BookingEndsAt { get; private set; }

    /// <summary>
    /// The maximum number of tickets a single buyer may hold for this event, summed across their
    /// active and converted holds. <see langword="null"/> means no limit. Enforced by Inventory at
    /// hold-placement time, propagated the same way as <see cref="BookingEndsAt"/>.
    /// </summary>
    public int? MaxTicketsPerBuyer { get; private set; }

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
    /// <returns>A new <see cref="Event"/> in <see cref="EventStatus.Draft"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="endsAt"/> is not after <paramref name="startsAt"/>.</exception>
    public static Event Create(
        Guid tenantId,
        string title,
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
        int? maxTicketsPerBuyer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine1);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        if (endsAt <= startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end time must be after the start time.");
        }

        return new Event(
            Guid.CreateVersion7(),
            tenantId,
            title,
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
            maxTicketsPerBuyer);
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
    /// Sets the event's descriptive/promotional details, dates, and contact/social info. Only
    /// permitted while the event is still a <see cref="EventStatus.Draft"/> — editing details
    /// after publish would need to re-notify buyers of material changes, which this pass does not
    /// implement (this includes <paramref name="bookingEndsAt"/> — it cannot be changed once
    /// published in this pass).
    /// </summary>
    /// <param name="description">Marketing description.</param>
    /// <param name="category">Free-text category.</param>
    /// <param name="endsAt">Scheduled end time (UTC) — must be after <see cref="StartsAt"/>.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if different from <see cref="StartsAt"/>.</param>
    /// <param name="onSaleAt">Display-only sales-window start (UTC).</param>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC) — see <see cref="BookingEndsAt"/>.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit — see <see cref="MaxTicketsPerBuyer"/>.</param>
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
    /// <exception cref="InvalidOperationException">The event is not a draft.</exception>
    public void UpdateDetails(
        string? description,
        string? category,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? onSaleAt,
        DateTimeOffset? bookingEndsAt,
        int? maxTicketsPerBuyer,
        string? ageRestriction,
        string? bannerImageUrl,
        string? videoUrl,
        string? contactPhone,
        string? contactMobile,
        string? contactEmail,
        string? websiteUrl,
        IEnumerable<(string Platform, string Url)> socialLinks)
    {
        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft event's details can be changed.");
        }

        if (endsAt <= StartsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end time must be after the start time.");
        }

        if (onSaleAt is not null && bookingEndsAt is not null && bookingEndsAt <= onSaleAt)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingEndsAt), "The booking cutoff must be after the on-sale time.");
        }

        Description = description;
        Category = category;
        EndsAt = endsAt;
        DoorsOpenAt = doorsOpenAt;
        OnSaleAt = onSaleAt;
        BookingEndsAt = bookingEndsAt;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
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
    /// Whether this event is visible to the given caller: everyone can see a
    /// <see cref="EventStatus.Published"/> event (and beyond); only the owning tenant can see a
    /// <see cref="EventStatus.Draft"/> event, never an anonymous caller or another tenant.
    /// </summary>
    /// <param name="callerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
    /// <returns><see langword="true"/> if the caller may see this event.</returns>
    public bool IsVisibleTo(Guid? callerTenantId) =>
        Status != EventStatus.Draft || (callerTenantId is not null && callerTenantId == TenantId);
}
