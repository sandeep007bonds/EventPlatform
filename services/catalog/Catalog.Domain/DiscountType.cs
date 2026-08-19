namespace Catalog.Domain;

/// <summary>How a <see cref="PromoCode"/>'s <see cref="PromoCode.DiscountValue"/> is interpreted.</summary>
public enum DiscountType
{
    /// <summary>A percentage off the eligible lines' subtotal — the value is a percentage in (0, 100].</summary>
    Percentage,

    /// <summary>A flat amount off, in major currency units (e.g. 250 = ₹250 / $250).</summary>
    FixedAmount,
}
