namespace Ordering.Domain;

/// <summary>
/// The money breakdown for an order: what the tickets cost, what came off, what tax was added, and
/// what the buyer actually pays. Every amount is in minor currency units.
/// </summary>
/// <param name="SubtotalMinor">Sum of the line prices, before any discount or tax.</param>
/// <param name="DiscountMinor">Amount taken off by a promo code. Zero when no code applied.</param>
/// <param name="TaxMinor">Tax charged on the post-discount subtotal. Zero when the event is untaxed.</param>
/// <param name="TotalMinor">What the buyer pays: subtotal − discount + tax.</param>
public sealed record OrderPricing(long SubtotalMinor, long DiscountMinor, long TaxMinor, long TotalMinor);
