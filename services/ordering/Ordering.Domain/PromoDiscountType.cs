namespace Ordering.Domain;

/// <summary>
/// How a promo code's value is interpreted when discounting an order.
/// </summary>
/// <remarks>
/// Deliberately a separate enum from Catalog's own <c>DiscountType</c> rather than a shared
/// contract type: Ordering reads promo codes over HTTP, not through a project reference, and the
/// two services are versioned independently. This mirrors how <c>HoldLineSnapshot</c> is Ordering's
/// own narrow view of Inventory's hold rather than Inventory's type.
/// </remarks>
public enum PromoDiscountType
{
    /// <summary>A percentage off the eligible lines' subtotal.</summary>
    Percentage,

    /// <summary>A flat amount off, expressed in major currency units.</summary>
    FixedAmount,
}
