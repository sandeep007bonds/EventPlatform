namespace Catalog.Domain;

/// <summary>
/// One <see cref="TicketType"/> a <see cref="PromoCode"/> is restricted to. A code with **no** rows
/// applies to every line in the order — the absence of restrictions is the unrestricted case, so an
/// organizer never has to enumerate every type just to discount the whole order.
/// </summary>
/// <remarks>
/// Bound by <b>id</b> rather than by tier name. The name was a string doing identity's work: it
/// matched only because comparison happened to be case-insensitive, it silently stopped matching
/// when a type was renamed, and nothing joined it to anything. Now that a line carries its
/// <see cref="TicketType"/> id from Inventory all the way through to the order, the restriction can
/// name the same thing.
/// </remarks>
public sealed class PromoCodeTier
{
    internal PromoCodeTier(Guid id, Guid promoCodeId, Guid ticketTypeId)
    {
        Id = id;
        PromoCodeId = promoCodeId;
        TicketTypeId = ticketTypeId;
    }

    // Parameterless ctor for EF Core materialization.
    private PromoCodeTier()
    {
    }

    /// <summary>Unique id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The promo code this restriction belongs to.</summary>
    public Guid PromoCodeId { get; private set; }

    /// <summary>The ticket type this code may be applied to.</summary>
    public Guid TicketTypeId { get; private set; }
}
