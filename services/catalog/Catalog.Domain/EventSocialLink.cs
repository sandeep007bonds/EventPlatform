namespace Catalog.Domain;

/// <summary>
/// A single social-media link on an <see cref="Event"/> — an open (platform, URL) pair rather
/// than fixed platform columns, so a new platform never needs a schema change. When an event has
/// any links of its own, they replace the owning <see cref="EventGroup"/>'s defaults entirely
/// rather than merging with them.
/// </summary>
public sealed class EventSocialLink
{
    internal EventSocialLink(Guid id, Guid eventId, string platform, string url)
    {
        Id = id;
        EventId = eventId;
        Platform = platform;
        Url = url;
    }

    // Parameterless ctor for EF Core materialization.
    private EventSocialLink()
    {
    }

    /// <summary>Unique id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event this link belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Free-text platform name (e.g. <c>Instagram</c>, <c>X</c>, <c>TikTok</c>).</summary>
    public string Platform { get; private set; } = default!;

    /// <summary>The link URL.</summary>
    public string Url { get; private set; } = default!;
}
