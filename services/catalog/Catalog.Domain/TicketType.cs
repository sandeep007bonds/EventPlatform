namespace Catalog.Domain;

/// <summary>
/// A named, priced kind of ticket for one <see cref="Event"/> — "Gold", "Early Bird", "Late
/// Release". Seat-map sections reference one; a section supplies the capacity and the geometry,
/// the type supplies what it is called, what it costs and the rules for selling it.
/// </summary>
/// <remarks>
/// <para>
/// This replaces a free-text <c>PriceTier</c> string denormalised across seats, general-admission
/// sections, promo-code scoping and order lines. That string was doing identity's job without
/// identity's guarantees: <c>"Gold"</c> and <c>"gold"</c> matched only because comparison happened
/// to be case-insensitive, <c>"Golden"</c> was silently a different tier, a tier could not be
/// renamed without orphaning every reference to it, and there was nowhere to record a per-type
/// sales window, buyer limit or description.
/// </para>
/// <para>
/// Unlike <see cref="PromoCode"/>, which deliberately has no edit-after-create, a ticket type
/// <b>is</b> editable. The reasoning differs rather than being inconsistent: an advertised discount
/// code must not silently change what it is worth, whereas repricing a tier or renaming it is
/// ordinary commercial work an organizer does all season.
/// </para>
/// </remarks>
public sealed class TicketType
{
    // Parameterless ctor for EF Core materialization.
    private TicketType()
    {
    }

    private TicketType(
        Guid id,
        Guid eventId,
        Guid tenantId,
        string name,
        long priceMinor,
        string? description,
        DateTimeOffset? salesStartsAt,
        DateTimeOffset? salesEndsAt,
        int? maxPerBuyer,
        int sortOrder)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        Name = name;
        PriceMinor = priceMinor;
        Description = description;
        SalesStartsAt = salesStartsAt;
        SalesEndsAt = salesEndsAt;
        MaxPerBuyer = maxPerBuyer;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique ticket-type id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event this type belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// What buyers see this ticket called. Unique within the event, compared case-insensitively —
    /// the invariant that stops "Gold" and "gold" becoming two types.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Price per ticket in minor currency units, in the event's currency.
    /// </summary>
    /// <remarks>
    /// Minor units, and the single place this price is stored. The seat map used to hold a
    /// <c>decimal</c> that Inventory converted with <c>× 100</c> at provisioning time — two
    /// representations of the same money, and a conversion that assumes a 2-decimal currency
    /// (tracker T11). One long, in the same units the rest of the money model already uses.
    /// </remarks>
    public long PriceMinor { get; private set; }

    /// <summary>Buyer-facing note on what this ticket includes. Optional.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Earliest instant this type may be sold, or <see langword="null"/> for no lower bound of its
    /// own. Narrows the event's own on-sale window rather than widening it — the event's bounds
    /// still apply, and Inventory enforces those.
    /// </summary>
    public DateTimeOffset? SalesStartsAt { get; private set; }

    /// <summary>Latest instant this type may be sold, or <see langword="null"/>. Narrows, never widens.</summary>
    public DateTimeOffset? SalesEndsAt { get; private set; }

    /// <summary>
    /// Cap on how many of this type one buyer may hold, or <see langword="null"/> for no per-type
    /// cap. Distinct from <see cref="Event.MaxTicketsPerBuyer"/>, which caps the event overall.
    /// </summary>
    public int? MaxPerBuyer { get; private set; }

    /// <summary>Display order in the buyer's list. Lower sorts first.</summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether this type is still offered. Deactivating retires it without deleting it, so seats
    /// and orders that reference it keep resolving.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>When the type was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a ticket type for an event.</summary>
    /// <param name="eventId">The event this type belongs to.</param>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="name">Buyer-facing name; unique within the event.</param>
    /// <param name="priceMinor">Price per ticket in minor units (non-negative).</param>
    /// <param name="description">Buyer-facing note. Optional.</param>
    /// <param name="salesStartsAt">Earliest sellable instant, or <see langword="null"/>.</param>
    /// <param name="salesEndsAt">Latest sellable instant, or <see langword="null"/>.</param>
    /// <param name="maxPerBuyer">Per-buyer cap for this type, or <see langword="null"/>.</param>
    /// <param name="sortOrder">Display order; lower sorts first.</param>
    /// <returns>A new, active <see cref="TicketType"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="priceMinor"/> is negative, <paramref name="maxPerBuyer"/> is not positive,
    /// or <paramref name="salesEndsAt"/> is not after <paramref name="salesStartsAt"/>.
    /// </exception>
    public static TicketType Create(
        Guid eventId,
        Guid tenantId,
        string name,
        long priceMinor,
        string? description = null,
        DateTimeOffset? salesStartsAt = null,
        DateTimeOffset? salesEndsAt = null,
        int? maxPerBuyer = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EnsurePriceIsSellable(priceMinor);
        EnsureRulesAreCoherent(salesStartsAt, salesEndsAt, maxPerBuyer);

        return new TicketType(
            Guid.CreateVersion7(),
            eventId,
            tenantId,
            name.Trim(),
            priceMinor,
            description,
            salesStartsAt,
            salesEndsAt,
            maxPerBuyer,
            sortOrder);
    }

    /// <summary>
    /// Renames the type. Safe at any event status: the name is display only, and every reference
    /// to this type is by id.
    /// </summary>
    /// <param name="name">The new name; must still be unique within the event.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or blank.</exception>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    /// <summary>
    /// Changes the price. <b>Only permitted while the event is still a draft</b> — the caller
    /// enforces that and passes the event's status in.
    /// </summary>
    /// <remarks>
    /// Inventory holds its own copy of the price, taken at provisioning time. Until a published
    /// event's repricing is propagated to it, changing the number here would move what the
    /// storefront displays while leaving what the buyer is actually charged untouched — a worse
    /// failure than refusing the edit, because it is silent and it is about money.
    /// </remarks>
    /// <param name="priceMinor">The new price in minor units (non-negative).</param>
    /// <param name="eventIsDraft">Whether the owning event is still a draft.</param>
    /// <exception cref="InvalidOperationException">The event is no longer a draft.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="priceMinor"/> is negative.</exception>
    public void Reprice(long priceMinor, bool eventIsDraft)
    {
        if (!eventIsDraft)
        {
            throw new InvalidOperationException(
                "A ticket type's price can only be changed while its event is still a draft.");
        }

        EnsurePriceIsSellable(priceMinor);
        PriceMinor = priceMinor;
    }

    /// <summary>
    /// Updates the selling rules and presentation. Safe at any event status — none of these change
    /// what an existing holder was charged.
    /// </summary>
    /// <param name="description">Buyer-facing note, or <see langword="null"/> to clear it.</param>
    /// <param name="salesStartsAt">Earliest sellable instant, or <see langword="null"/>.</param>
    /// <param name="salesEndsAt">Latest sellable instant, or <see langword="null"/>.</param>
    /// <param name="maxPerBuyer">Per-buyer cap, or <see langword="null"/>.</param>
    /// <param name="sortOrder">Display order.</param>
    /// <exception cref="ArgumentOutOfRangeException">The window or cap is incoherent.</exception>
    public void UpdateRules(
        string? description,
        DateTimeOffset? salesStartsAt,
        DateTimeOffset? salesEndsAt,
        int? maxPerBuyer,
        int sortOrder)
    {
        EnsureRulesAreCoherent(salesStartsAt, salesEndsAt, maxPerBuyer);

        Description = description;
        SalesStartsAt = salesStartsAt;
        SalesEndsAt = salesEndsAt;
        MaxPerBuyer = maxPerBuyer;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Retires the type so it is no longer offered. Never deleted: seats, orders and tickets
    /// reference it by id, and those references have to keep resolving.
    /// </summary>
    public void Deactivate() => IsActive = false;

    private static void EnsurePriceIsSellable(long priceMinor)
    {
        if (priceMinor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(priceMinor),
                priceMinor,
                "A ticket price cannot be negative.");
        }
    }

    private static void EnsureRulesAreCoherent(
        DateTimeOffset? salesStartsAt,
        DateTimeOffset? salesEndsAt,
        int? maxPerBuyer)
    {
        if (salesStartsAt is not null && salesEndsAt is not null && salesEndsAt <= salesStartsAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(salesEndsAt),
                salesEndsAt,
                "The sales window must end after it starts.");
        }

        if (maxPerBuyer is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxPerBuyer),
                maxPerBuyer,
                "A per-buyer cap must be positive; use null for no cap.");
        }
    }
}
