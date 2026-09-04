namespace Catalog.Domain;

/// <summary>
/// An organizer-created discount code for one <see cref="Event"/>. Catalog owns the *definition*
/// (what the code is worth, when it is valid, which tiers it touches, how often it may be used);
/// Ordering owns the arithmetic and the redemption count, because it owns orders and totals.
/// </summary>
/// <remarks>
/// There is deliberately no edit-after-create: an advertised code must not silently change what
/// it is worth. Deactivate it and make another.
/// A code that has already been advertised should not silently change what it is worth; deactivate
/// it and create another instead.
/// </remarks>
public sealed class PromoCode
{
    private readonly List<PromoCodeTier> _tiers = new();

    // Parameterless ctor for EF Core materialization.
    private PromoCode()
    {
    }

    private PromoCode(
        Guid id,
        Guid eventId,
        Guid tenantId,
        string code,
        string? description,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo,
        bool isPublic,
        int? maxRedemptions,
        int? maxRedemptionsPerBuyer)
    {
        Id = id;
        EventId = eventId;
        TenantId = tenantId;
        Code = code;
        Description = description;
        DiscountType = discountType;
        DiscountValue = discountValue;
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsPublic = isPublic;
        MaxRedemptions = maxRedemptions;
        MaxRedemptionsPerBuyer = maxRedemptionsPerBuyer;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique promo-code id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The event this code discounts.</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The code a buyer types, stored upper-invariant. Matching is therefore case-insensitive
    /// without needing a case-insensitive index or collation: every lookup upper-cases first.
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>Organizer-facing note on what this code is for. Never shown to buyers.</summary>
    public string? Description { get; private set; }

    /// <summary>Whether <see cref="DiscountValue"/> is a percentage or a flat amount.</summary>
    public DiscountType DiscountType { get; private set; }

    /// <summary>
    /// The discount magnitude: a percentage in (0, 100] when <see cref="DiscountType"/> is
    /// <see cref="DiscountType.Percentage"/>, otherwise a flat amount in **major** currency units
    /// (converted to minor units by the caller doing the arithmetic).
    /// </summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>Earliest instant the code may be redeemed. <see langword="null"/> means no lower bound.</summary>
    public DateTimeOffset? ValidFrom { get; private set; }

    /// <summary>Latest instant the code may be redeemed. <see langword="null"/> means no upper bound.</summary>
    public DateTimeOffset? ValidTo { get; private set; }

    /// <summary>
    /// Whether buyers may discover this code without being told it. Public codes are listed on the
    /// checkout page for anyone holding seats; private ones only work if typed in.
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>
    /// Cap on how many orders may ever redeem this code. <see langword="null"/> means unlimited.
    /// Enforced by Ordering, which owns the orders that would be counted.
    /// </summary>
    public int? MaxRedemptions { get; private set; }

    /// <summary>
    /// Cap on how many orders a single buyer may redeem this code across. <see langword="null"/>
    /// means unlimited. Enforced by Ordering.
    /// </summary>
    public int? MaxRedemptionsPerBuyer { get; private set; }

    /// <summary>Whether the code is still usable. Deactivating is the only way to retire one.</summary>
    public bool IsActive { get; private set; }

    /// <summary>When the code was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// The ticket types this code applies to. **Empty means every type** — see
    /// <see cref="PromoCodeTier"/>.
    /// </summary>
    public IReadOnlyCollection<PromoCodeTier> Tiers => _tiers;

    /// <summary>Creates a promo code for an event.</summary>
    /// <param name="eventId">The event being discounted.</param>
    /// <param name="tenantId">Owning tenant (organizer).</param>
    /// <param name="code">The code buyers type. Stored upper-invariant.</param>
    /// <param name="description">Organizer-facing note. Optional.</param>
    /// <param name="discountType">Percentage or flat amount.</param>
    /// <param name="discountValue">Percentage in (0, 100], or a flat amount in major units.</param>
    /// <param name="validFrom">Earliest redeemable instant, or <see langword="null"/>.</param>
    /// <param name="validTo">Latest redeemable instant, or <see langword="null"/>.</param>
    /// <param name="isPublic">Whether the code is listed to buyers rather than typed in.</param>
    /// <param name="maxRedemptions">Total redemption cap, or <see langword="null"/> for unlimited.</param>
    /// <param name="maxRedemptionsPerBuyer">Per-buyer cap, or <see langword="null"/> for unlimited.</param>
    /// <param name="ticketTypeIds">Ticket types to restrict to. Empty or <see langword="null"/> applies to all of them.</param>
    /// <returns>A new <see cref="PromoCode"/>.</returns>
    public static PromoCode Create(
        Guid eventId,
        Guid tenantId,
        string code,
        string? description,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset? validFrom,
        DateTimeOffset? validTo,
        bool isPublic,
        int? maxRedemptions,
        int? maxRedemptionsPerBuyer,
        IEnumerable<Guid>? ticketTypeIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (discountType == DiscountType.Percentage && discountValue is <= 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountValue),
                discountValue,
                "A percentage discount must be greater than 0 and at most 100.");
        }

        if (discountType == DiscountType.FixedAmount && discountValue <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountValue),
                discountValue,
                "A fixed discount must be greater than zero.");
        }

        if (validFrom is not null && validTo is not null && validTo <= validFrom)
        {
            throw new ArgumentOutOfRangeException(
                nameof(validTo),
                validTo,
                "The end of the validity window must be later than its start.");
        }

        if (maxRedemptions is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRedemptions),
                maxRedemptions,
                "The redemption cap must be greater than zero when set.");
        }

        if (maxRedemptionsPerBuyer is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRedemptionsPerBuyer),
                maxRedemptionsPerBuyer,
                "The per-buyer redemption cap must be greater than zero when set.");
        }

        var promoCode = new PromoCode(
            Guid.CreateVersion7(),
            eventId,
            tenantId,
            code.Trim().ToUpperInvariant(),
            description,
            discountType,
            discountValue,
            validFrom,
            validTo,
            isPublic,
            maxRedemptions,
            maxRedemptionsPerBuyer);

        // Distinct: a duplicated type would double nothing (eligibility is a set membership test)
        // but would show up twice in the organizer's own listing, which reads as a bug.
        foreach (var ticketTypeId in (ticketTypeIds ?? []).Where(id => id != Guid.Empty).Distinct())
        {
            promoCode._tiers.Add(new PromoCodeTier(Guid.CreateVersion7(), promoCode.Id, ticketTypeId));
        }

        return promoCode;
    }

    /// <summary>
    /// Retires the code. Idempotent — deactivating an already-inactive code is a no-op rather than
    /// an error, so a double-click on the organizer's Deactivate button doesn't 500.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Whether the code is active and <paramref name="now"/> falls inside its validity window.
    /// Says nothing about redemption caps — those are counted from orders, which Catalog cannot see.
    /// </summary>
    /// <param name="now">The instant to test, normally <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns><see langword="true"/> if the code is redeemable at that instant.</returns>
    public bool IsRedeemableAt(DateTimeOffset now) =>
        IsActive
        && (ValidFrom is null || now >= ValidFrom)
        && (ValidTo is null || now <= ValidTo);
}
