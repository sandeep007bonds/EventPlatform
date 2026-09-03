namespace Catalog.Domain;

/// <summary>
/// One price tier a <see cref="PromoCode"/> is restricted to. A code with **no** tier rows applies
/// to every line in the order — the absence of restrictions is the unrestricted case, so an
/// organizer never has to enumerate every tier just to discount the whole order.
/// </summary>
public sealed class PromoCodeTier
{
    internal PromoCodeTier(Guid id, Guid promoCodeId, string priceTier)
    {
        Id = id;
        PromoCodeId = promoCodeId;
        PriceTier = priceTier;
    }

    // Parameterless ctor for EF Core materialization.
    private PromoCodeTier()
    {
    }

    /// <summary>Unique id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The promo code this restriction belongs to.</summary>
    public Guid PromoCodeId { get; private set; }

    /// <summary>
    /// The price-tier name, matching a <see cref="TicketType"/> name verbatim. A plain string rather
    /// than a foreign key, for the same reason <see cref="Seat.PriceTier"/> is: tiers are named on
    /// sections, not modelled as their own entity.
    /// </summary>
    public string PriceTier { get; private set; } = default!;
}
