namespace Catalog.Domain;

/// <summary>
/// A single social-media link on an <see cref="EventGroup"/> — an open (platform, URL) pair
/// rather than fixed platform columns, so a new platform never needs a schema change.
/// </summary>
public sealed class EventGroupSocialLink
{
    internal EventGroupSocialLink(Guid id, Guid eventGroupId, string platform, string url)
    {
        Id = id;
        EventGroupId = eventGroupId;
        Platform = platform;
        Url = url;
    }

    // Parameterless ctor for EF Core materialization.
    private EventGroupSocialLink()
    {
    }

    /// <summary>Unique id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event group this link belongs to.</summary>
    public Guid EventGroupId { get; private set; }

    /// <summary>Free-text platform name (e.g. <c>Instagram</c>, <c>X</c>, <c>TikTok</c>).</summary>
    public string Platform { get; private set; } = default!;

    /// <summary>The link URL.</summary>
    public string Url { get; private set; } = default!;
}
