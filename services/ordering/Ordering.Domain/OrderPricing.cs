namespace Ordering.Domain;

/// <summary>
/// The money breakdown for an order: what the tickets cost, what came off, what was added, and what
/// the buyer actually pays. Every amount is in minor currency units.
/// </summary>
/// <param name="SubtotalMinor">Sum of the line prices, before any discount, fee or tax.</param>
/// <param name="DiscountMinor">Amount taken off by a promo code. Zero when no code applied.</param>
/// <param name="BookingFeeMinor">The event's per-ticket booking fee times the ticket count. Zero when the event charges none.</param>
/// <param name="TaxMinor">Tax on the post-discount subtotal plus tax on the booking fee. Zero when the event is untaxed.</param>
/// <param name="TotalMinor">What the buyer pays: subtotal − discount + fee + tax.</param>
/// <param name="RefundableMinor">
/// What a full cancellation returns: subtotal − discount, plus the tax on that. Excludes the
/// booking fee and the tax charged on it, because the fee is not refundable — see
/// <see cref="OrderPricingCalculator"/>.
/// </param>
public sealed record OrderPricing(
    long SubtotalMinor,
    long DiscountMinor,
    long BookingFeeMinor,
    long TaxMinor,
    long TotalMinor,
    long RefundableMinor);
