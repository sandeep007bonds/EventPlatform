namespace Venues.Domain;

/// <summary>
/// Aggregate root for one seating configuration of a <see cref="Venue"/> — "end stage", "in the
/// round", "cricket". A venue usually has several, and each is a series of numbered versions.
/// </summary>
/// <remarks>
/// The map is the reusable asset this service exists to hold. An event does not draw a plan; it
/// points at a published version of one, and the same version serves every event that uses that
/// configuration. Editing is always against a draft, and there is at most one draft at a time —
/// two people editing two drafts of the same map would both be right and only one could win.
/// </remarks>
public sealed class SeatMap
{
    private readonly List<SeatMapVersion> _versions = new();

    // Parameterless ctor for EF Core materialization.
    private SeatMap()
    {
    }

    private SeatMap(Guid id, Guid venueId, Guid tenantId, string name)
    {
        Id = id;
        VenueId = venueId;
        TenantId = tenantId;
        Name = name;
        _versions.Add(new SeatMapVersion(Guid.CreateVersion7(), id, 1));
    }

    /// <summary>Unique seat-map id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The venue this map configures.</summary>
    public Guid VenueId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Configuration name (e.g. <c>End stage</c>).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>The version currently live, if any has been published.</summary>
    public int? PublishedVersionNumber { get; private set; }

    /// <summary>Every version of this map, published and draft.</summary>
    public IReadOnlyCollection<SeatMapVersion> Versions => _versions;

    /// <summary>The version being edited, if there is one.</summary>
    public SeatMapVersion? Draft => _versions.SingleOrDefault(v => v.Status == SeatMapVersionStatus.Draft);

    /// <summary>The version events should be provisioned from, if any.</summary>
    public SeatMapVersion? Published =>
        _versions.SingleOrDefault(v => v.Status == SeatMapVersionStatus.Published);

    /// <summary>Creates a new seat map with an empty version 1 ready to edit.</summary>
    /// <param name="venueId">The venue this map configures.</param>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="name">Configuration name.</param>
    /// <returns>The new seat map.</returns>
    public static SeatMap Create(Guid venueId, Guid tenantId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SeatMap(Guid.CreateVersion7(), venueId, tenantId, name);
    }

    /// <summary>Renames the configuration. Affects no version's contents.</summary>
    /// <param name="name">The new name.</param>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Opens a new draft, pre-filled with the published version's layout so a structural change
    /// starts from what is live rather than from an empty canvas.
    /// </summary>
    /// <returns>The new draft version.</returns>
    /// <exception cref="InvalidOperationException">A draft is already open.</exception>
    public SeatMapVersion StartNewDraft()
    {
        if (Draft is not null)
        {
            throw new InvalidOperationException(
                "This map already has an open draft. Publish or edit that one rather than starting another.");
        }

        var draft = new SeatMapVersion(Guid.CreateVersion7(), Id, _versions.Max(v => v.VersionNumber) + 1);

        if (Published is { } published)
        {
            draft.ReplaceLayout(published.ToLayout());
        }

        _versions.Add(draft);

        return draft;
    }

    /// <summary>Replaces the open draft's layout.</summary>
    /// <param name="layout">The complete layout to store.</param>
    /// <exception cref="InvalidOperationException">There is no open draft.</exception>
    public void SaveDraftLayout(SeatMapLayout layout)
    {
        var draft = Draft
            ?? throw new InvalidOperationException("This map has no open draft. Start a new version first.");

        draft.ReplaceLayout(layout);
    }

    /// <summary>
    /// Publishes the open draft and supersedes whatever was live. From here the draft's layout is
    /// immutable — see <see cref="SeatMapVersion"/> for why.
    /// </summary>
    /// <param name="publishedAt">The publication instant.</param>
    /// <returns>The now-published version.</returns>
    /// <exception cref="InvalidOperationException">There is no open draft, or it does not validate.</exception>
    public SeatMapVersion PublishDraft(DateTimeOffset publishedAt)
    {
        var draft = Draft
            ?? throw new InvalidOperationException("This map has no open draft to publish.");

        // Captured first, and for two reasons. A failed publish must leave the live version live
        // rather than taking the venue's map offline because someone tried an edit that did not
        // pass — and once the draft is published, "the published version" momentarily matches two
        // versions, so asking again here would find the wrong one.
        var previouslyPublished = Published;

        draft.Publish(publishedAt);
        previouslyPublished?.Supersede();

        PublishedVersionNumber = draft.VersionNumber;

        return draft;
    }
}
