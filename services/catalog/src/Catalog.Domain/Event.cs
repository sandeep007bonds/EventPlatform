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
}
