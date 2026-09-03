namespace Catalog.Domain;

/// <summary>
/// One performance of an <see cref="Event"/> — a specific night, in a specific venue, with its own
/// seat map and its own inventory.
/// </summary>
/// <remarks>
/// <b>This is the grain everything downstream hangs off.</b> Inventory provisions per session,
/// orders and tickets name one, and a scan is validated against one. Before sessions existed a
/// three-night run had to be three separate events with three separate seat maps and three separate
/// pages, and any report over them was aggregating things the model said were unrelated.
/// <para>
/// A session is not a tour leg. A leg is a different city, a different venue and a separately
/// advertised event — that is what <see cref="EventGroup"/> is for. Sessions are several
/// performances of the <i>same</i> event.
/// </para>
/// <para>
/// The seat map is a <b>Venue</b> seat-map version, referenced by id and never copied. Two nights
/// of one run can use different configurations — end stage on Friday, in the round on Saturday —
/// and a published version is immutable, so the seats a ticket names cannot move underneath it.
/// </para>
/// </remarks>
public sealed class EventSession
{
    private readonly List<SessionAllocation> _allocations = new();

    internal EventSession(
        Guid id,
        Guid eventId,
        Guid tenantId,
        string? name,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? bookingEndsAt)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        Name = name;
        StartsAt = startsAt;
        EndsAt = endsAt;
        DoorsOpenAt = doorsOpenAt;
        BookingEndsAt = bookingEndsAt;
        Status = EventSessionStatus.Draft;
    }

    // Parameterless ctor for EF Core materialization.
    private EventSession()
    {
    }

    /// <summary>Unique session id (UUID v7 — time-sortable). Stable across services.</summary>
    public Guid Id { get; private set; }

    /// <summary>The event this is a performance of.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer), copied from the event so downstream rows carry it.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// What to call this performance when there is more than one — <c>Matinee</c>, <c>Opening
    /// night</c>. <see langword="null"/> for a single-performance event, where a name would only be
    /// noise on the page.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>Scheduled start (UTC).</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Scheduled end (UTC).</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Doors-open time (UTC), if different from the start.</summary>
    public DateTimeOffset? DoorsOpenAt { get; private set; }

    /// <summary>
    /// Booking cutoff (UTC) — after this, Inventory rejects new holds for this performance. Per
    /// session rather than per event because the useful rule is "book until two hours before
    /// <i>this</i> show", which means a different instant every night.
    /// </summary>
    public DateTimeOffset? BookingEndsAt { get; private set; }

    /// <summary>Lifecycle state.</summary>
    public EventSessionStatus Status { get; private set; }

    /// <summary>
    /// Whether an organizer has manually paused sales for this performance. Per session, so one
    /// night can be pulled without pulling the run; <see cref="Event.PauseSales"/> fans out to all
    /// of them when the whole event has to stop.
    /// </summary>
    public bool SalesPaused { get; private set; }

    /// <summary>The Venue this performance happens at, once one is attached.</summary>
    public Guid? VenueId { get; private set; }

    /// <summary>The Venue seat map used, once one is attached.</summary>
    public Guid? SeatMapId { get; private set; }

    /// <summary>
    /// The specific, immutable seat-map <i>version</i> used. Pinning the version rather than the
    /// map is what stops a later venue reconfiguration moving the seats a sold ticket names.
    /// </summary>
    public Guid? SeatMapVersionId { get; private set; }

    /// <summary>That version's number, carried for display and for reading it back from Venue.</summary>
    public int? SeatMapVersionNumber { get; private set; }

    /// <summary>
    /// A copy of the venue's name, city and time zone for display. See <see cref="VenueSnapshot"/>
    /// — it is a cache, and nothing is ever decided from it.
    /// </summary>
    public VenueSnapshot? Venue { get; private set; }

    /// <summary>Which block is sold as which ticket type, for this performance.</summary>
    public IReadOnlyCollection<SessionAllocation> Allocations => _allocations;

    /// <summary>Whether this performance has everything it needs to go on sale.</summary>
    public bool IsSellable => SeatMapVersionId is not null && _allocations.Count > 0;

    /// <summary>Moves this performance in time. Only while it is a draft.</summary>
    /// <param name="startsAt">Scheduled start (UTC).</param>
    /// <param name="endsAt">Scheduled end (UTC) — must be after <paramref name="startsAt"/>.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if different from the start.</param>
    /// <param name="bookingEndsAt">Booking cutoff (UTC) — see <see cref="BookingEndsAt"/>.</param>
    /// <exception cref="InvalidOperationException">The performance is not a draft.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A date is out of range.</exception>
    public void Reschedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? bookingEndsAt)
    {
        EnsureDraft();
        ValidateTimes(startsAt, endsAt, doorsOpenAt, bookingEndsAt);

        StartsAt = startsAt;
        EndsAt = endsAt;
        DoorsOpenAt = doorsOpenAt;
        BookingEndsAt = bookingEndsAt;
    }

    /// <summary>Renames the performance.</summary>
    /// <param name="name">The new name, or <see langword="null"/> to clear it.</param>
    public void Rename(string? name) => Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    /// <summary>
    /// Points this performance at a published Venue seat-map version, and caches the venue's display
    /// details. Only while it is a draft: changing the map after publish would move the seats sold
    /// tickets name.
    /// </summary>
    /// <remarks>
    /// Changing the map clears the allocations. They bind to section codes that belong to the old
    /// version, and silently keeping ones that happen to match would leave the rest missing without
    /// saying so — clearing forces the organizer past the allocation step again, which is where the
    /// mistake would otherwise be found.
    /// </remarks>
    /// <param name="venueId">The venue.</param>
    /// <param name="seatMapId">The seat map.</param>
    /// <param name="seatMapVersionId">The specific published version.</param>
    /// <param name="seatMapVersionNumber">That version's number.</param>
    /// <param name="venue">Display details copied from the venue.</param>
    /// <exception cref="InvalidOperationException">The performance is not a draft.</exception>
    public void AttachSeatMap(
        Guid venueId,
        Guid seatMapId,
        Guid seatMapVersionId,
        int seatMapVersionNumber,
        VenueSnapshot venue)
    {
        ArgumentNullException.ThrowIfNull(venue);
        EnsureDraft();

        var changed = SeatMapVersionId != seatMapVersionId;

        VenueId = venueId;
        SeatMapId = seatMapId;
        SeatMapVersionId = seatMapVersionId;
        SeatMapVersionNumber = seatMapVersionNumber;
        Venue = venue;

        if (changed)
        {
            _allocations.Clear();
        }
    }

    /// <summary>
    /// Replaces the whole allocation map — which block is sold as which ticket type.
    /// </summary>
    /// <remarks>
    /// Wholesale, like the seat-map layout it mirrors: the caller knows every block in the version
    /// it is looking at, and a partial update would leave "which blocks are still unassigned"
    /// unanswerable without re-reading everything anyway.
    /// </remarks>
    /// <param name="allocations">Section/area code paired with the ticket type it sells as.</param>
    /// <exception cref="InvalidOperationException">
    /// The performance is not a draft, or a code appears twice.
    /// </exception>
    public void SetAllocations(IEnumerable<(string Code, Guid TicketTypeId)> allocations)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        EnsureDraft();

        var materialized = allocations.ToList();

        var duplicate = materialized
            .GroupBy(a => a.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Block '{duplicate.Key}' is allocated more than once for this performance.");
        }

        _allocations.Clear();
        _allocations.AddRange(materialized.Select(a =>
            new SessionAllocation(Guid.CreateVersion7(), Id, a.Code, a.TicketTypeId)));
    }

    /// <summary>
    /// Takes the performance on sale. Inventory is provisioned from this moment, so it needs a seat
    /// map and something to sell.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The performance is not a draft, or has no seat map or no allocations.
    /// </exception>
    public void Publish()
    {
        EnsureDraft();

        if (SeatMapVersionId is null)
        {
            throw new InvalidOperationException(
                "This performance has no seat map. Attach a published seat-map version before publishing it.");
        }

        if (_allocations.Count == 0)
        {
            throw new InvalidOperationException(
                "This performance sells nothing: no block has been allocated to a ticket type.");
        }

        Status = EventSessionStatus.Published;
    }

    /// <summary>Calls the performance off. Kept rather than deleted — tickets reference it.</summary>
    /// <exception cref="InvalidOperationException">It is already cancelled.</exception>
    public void Cancel()
    {
        if (Status == EventSessionStatus.Cancelled)
        {
            throw new InvalidOperationException("This performance is already cancelled.");
        }

        Status = EventSessionStatus.Cancelled;
    }

    /// <summary>Pauses sales for this performance without affecting placed holds or tickets.</summary>
    /// <exception cref="InvalidOperationException">It is not published, or is already paused.</exception>
    public void PauseSales()
    {
        if (Status != EventSessionStatus.Published)
        {
            throw new InvalidOperationException("Only a published performance's sales can be paused.");
        }

        if (SalesPaused)
        {
            throw new InvalidOperationException("Sales are already paused for this performance.");
        }

        SalesPaused = true;
    }

    /// <summary>Resumes sales for a paused performance.</summary>
    /// <exception cref="InvalidOperationException">It is not published, or is not paused.</exception>
    public void ResumeSales()
    {
        if (Status != EventSessionStatus.Published)
        {
            throw new InvalidOperationException("Only a published performance's sales can be resumed.");
        }

        if (!SalesPaused)
        {
            throw new InvalidOperationException("Sales are not paused for this performance.");
        }

        SalesPaused = false;
    }

    /// <summary>Whether this performance's times overlap another's.</summary>
    /// <param name="startsAt">The other start.</param>
    /// <param name="endsAt">The other end.</param>
    /// <returns><see langword="true"/> if the two ranges intersect.</returns>
    public bool Overlaps(DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        startsAt < EndsAt && StartsAt < endsAt;

    internal static void ValidateTimes(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset? bookingEndsAt)
    {
        if (endsAt <= startsAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAt), "The end time must be after the start time.");
        }

        if (doorsOpenAt is not null && doorsOpenAt > startsAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(doorsOpenAt),
                "Doors cannot open after the performance starts.");
        }

        // Selling a ticket once the doors are open is a different feature (walk-ups), not something
        // the cutoff should quietly allow by being set past the start.
        if (bookingEndsAt is not null && bookingEndsAt > startsAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookingEndsAt),
                "The booking cutoff must not be later than the performance's start time.");
        }
    }

    // Sets the paused flag without the published-state guard, so an event-wide pause can sweep
    // every session including ones that are not published and would otherwise throw.
    internal void SetSalesPaused(bool paused) => SalesPaused = paused;

    private void EnsureDraft()
    {
        if (Status != EventSessionStatus.Draft)
        {
            throw new InvalidOperationException(
                "A performance's times, venue and pricing can only be changed while it is a draft.");
        }
    }
}
