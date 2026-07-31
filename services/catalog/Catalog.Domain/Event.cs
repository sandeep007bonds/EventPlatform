namespace Catalog.Domain;

/// <summary>
/// The Catalog aggregate root: a sellable event held at a venue. Enforces its own
/// lifecycle invariants (a draft becomes published, etc.).
/// </summary>
public sealed class Event
{
    // Parameterless ctor for EF Core materialization.
    private Event()
    {
    }

    private Event(Guid id, Guid tenantId, Guid venueId, string title, DateTimeOffset startsAt, string currency)
    {
        Id = id;
        TenantId = tenantId;
        VenueId = venueId;
        Title = title;
        StartsAt = startsAt;
        Currency = currency;
        Status = EventStatus.Draft;
    }

    /// <summary>Unique event id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Venue at which the event is held.</summary>
    public Guid VenueId { get; private set; }

    /// <summary>Event title.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Scheduled start time (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Pricing currency (ISO 4217, e.g. <c>USD</c>).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Current lifecycle status.</summary>
    public EventStatus Status { get; private set; }

    /// <summary>Marketing description shown on the public event page.</summary>
    public string? Description { get; private set; }

    /// <summary>Free-text category (e.g. "Concert", "Comedy") — not a taxonomy table in this pass.</summary>
    public string? Category { get; private set; }

    /// <summary>Scheduled end time (UTC), if known.</summary>
    public DateTimeOffset? EndsAt { get; private set; }

    /// <summary>Doors-open time (UTC), if different from <see cref="StartsAt"/>.</summary>
    public DateTimeOffset? DoorsOpenAt { get; private set; }

    /// <summary>
    /// Display-only sales-window start (UTC). Not enforced by <see cref="Publish"/> or any status
    /// transition in this pass — purely informational on the public event page.
    /// </summary>
    public DateTimeOffset? OnSaleAt { get; private set; }

    /// <summary>Display-only sales-window end (UTC) — see <see cref="OnSaleAt"/>.</summary>
    public DateTimeOffset? OffSaleAt { get; private set; }

    /// <summary>Free-text age restriction (e.g. "18+", "All ages"), if any.</summary>
    public string? AgeRestriction { get; private set; }

    /// <summary>
    /// URL of the banner image shown on the public event page. Set by pasting the URL returned
    /// from the Media service's upload endpoint — this service has no awareness of blob storage.
    /// </summary>
    public string? BannerImageUrl { get; private set; }

    /// <summary>Video embed URL (e.g. YouTube/Vimeo link) — not a hosted/uploaded video file.</summary>
    public string? VideoUrl { get; private set; }

    /// <summary>Creates a new draft event for the given tenant.</summary>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="venueId">Venue the event is held at.</param>
    /// <param name="title">Event title.</param>
    /// <param name="startsAt">Scheduled start (UTC).</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <returns>A new <see cref="Event"/> in <see cref="EventStatus.Draft"/>.</returns>
    public static Event Create(Guid tenantId, Guid venueId, string title, DateTimeOffset startsAt, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new Event(Guid.CreateVersion7(), tenantId, venueId, title, startsAt, currency);
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
    /// Sets the event's descriptive/promotional details. Only permitted while the event is still
    /// a <see cref="EventStatus.Draft"/> — editing details after publish would need to re-notify
    /// buyers of material changes, which this pass does not implement.
    /// </summary>
    /// <param name="description">Marketing description.</param>
    /// <param name="category">Free-text category.</param>
    /// <param name="endsAt">Scheduled end time (UTC), if known.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if different from <see cref="StartsAt"/>.</param>
    /// <param name="onSaleAt">Display-only sales-window start (UTC).</param>
    /// <param name="offSaleAt">Display-only sales-window end (UTC).</param>
    /// <param name="ageRestriction">Free-text age restriction.</param>
    /// <param name="bannerImageUrl">Banner image URL (from the Media service's upload endpoint).</param>
    /// <param name="videoUrl">Video embed URL.</param>
    /// <exception cref="InvalidOperationException">The event is not a draft.</exception>
    public void UpdateDetails(
        string? description,
        string? category,
        DateTimeOffset? endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? onSaleAt,
        DateTimeOffset? offSaleAt,
        string? ageRestriction,
        string? bannerImageUrl,
        string? videoUrl)
    {
        if (Status != EventStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft event's details can be changed.");
        }

        if (endsAt is not null && endsAt <= StartsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end time must be after the start time.");
        }

        if (onSaleAt is not null && offSaleAt is not null && offSaleAt <= onSaleAt)
        {
            throw new ArgumentOutOfRangeException(nameof(offSaleAt), "The off-sale time must be after the on-sale time.");
        }

        Description = description;
        Category = category;
        EndsAt = endsAt;
        DoorsOpenAt = doorsOpenAt;
        OnSaleAt = onSaleAt;
        OffSaleAt = offSaleAt;
        AgeRestriction = ageRestriction;
        BannerImageUrl = bannerImageUrl;
        VideoUrl = videoUrl;
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
