namespace Catalog.Domain;

/// <summary>
/// A thin, optional grouping of multiple <see cref="Event"/>s under one organizer-facing heading
/// — a multi-city tour, a conference roadshow, a comedy circuit. Each grouped <see cref="Event"/>
/// (a "leg") is still created, published, seat-mapped, held, checked out, and ticketed exactly
/// like any standalone event; this exists only to cluster legs for display and navigation, plus
/// hold tour-wide defaults (dates, contact/social) that a leg may override.
/// </summary>
public sealed class EventGroup
{
    private readonly List<EventGroupSocialLink> _socialLinks = new();

    // Parameterless ctor for EF Core materialization.
    private EventGroup()
    {
    }

    private EventGroup(Guid id, Guid tenantId, string title)
    {
        Id = id;
        TenantId = tenantId;
        Title = title;
    }

    /// <summary>Unique event-group id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Group title (e.g. "Coldplay World Tour").</summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// Overall advertised start of the tour, independent of (and not derived from) any one leg's
    /// dates — organizers may set this before every leg exists.
    /// </summary>
    public DateTimeOffset? StartsAt { get; private set; }

    /// <summary>Overall advertised end of the tour — see <see cref="StartsAt"/>.</summary>
    public DateTimeOffset? EndsAt { get; private set; }

    /// <summary>Tour-wide default contact phone, used by a leg that sets no override.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Tour-wide default contact mobile number — see <see cref="ContactPhone"/>.</summary>
    public string? ContactMobile { get; private set; }

    /// <summary>Tour-wide default contact email — see <see cref="ContactPhone"/>.</summary>
    public string? ContactEmail { get; private set; }

    /// <summary>Tour-wide default website URL — see <see cref="ContactPhone"/>.</summary>
    public string? WebsiteUrl { get; private set; }

    /// <summary>
    /// Tour-wide default social links (open platform list), used by a leg that sets none of its
    /// own. See <see cref="EventGroupSocialLink"/>.
    /// </summary>
    public IReadOnlyCollection<EventGroupSocialLink> SocialLinks => _socialLinks;

    /// <summary>Creates a new event group for the given tenant.</summary>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="title">Group title.</param>
    /// <returns>A new <see cref="EventGroup"/>.</returns>
    public static EventGroup Create(Guid tenantId, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new EventGroup(Guid.CreateVersion7(), tenantId, title);
    }

    /// <summary>Updates the group's title, overall date range, and tour-wide contact/social defaults.</summary>
    /// <param name="title">Group title.</param>
    /// <param name="startsAt">Overall advertised start of the tour, if known.</param>
    /// <param name="endsAt">Overall advertised end of the tour, if known.</param>
    /// <param name="contactPhone">Tour-wide default contact phone.</param>
    /// <param name="contactMobile">Tour-wide default contact mobile number.</param>
    /// <param name="contactEmail">Tour-wide default contact email.</param>
    /// <param name="websiteUrl">Tour-wide default website URL.</param>
    /// <param name="socialLinks">Tour-wide default social links (platform, URL pairs); replaces the existing list.</param>
    public void Update(
        string title,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        string? contactPhone,
        string? contactMobile,
        string? contactEmail,
        string? websiteUrl,
        IEnumerable<(string Platform, string Url)> socialLinks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (startsAt is not null && endsAt is not null && endsAt <= startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end date must be after the start date.");
        }

        Title = title;
        StartsAt = startsAt;
        EndsAt = endsAt;
        ContactPhone = contactPhone;
        ContactMobile = contactMobile;
        ContactEmail = contactEmail;
        WebsiteUrl = websiteUrl;

        _socialLinks.Clear();
        _socialLinks.AddRange(socialLinks.Select(link => new EventGroupSocialLink(Guid.CreateVersion7(), Id, link.Platform, link.Url)));
    }
}
