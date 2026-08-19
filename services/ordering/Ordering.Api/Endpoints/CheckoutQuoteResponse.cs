namespace Ordering.Api.Endpoints;

/// <summary>
/// What a checkout would cost right now. Advisory: the saga re-prices from scratch at confirm time,
/// so a code that expires in between is caught there rather than silently honoured.
/// </summary>
/// <param name="SubtotalMinor">Sum of the held seats' prices, before discount or tax.</param>
/// <param name="DiscountMinor">What the promo code takes off. Zero when none applied or accepted.</param>
/// <param name="TaxMinor">Tax on the post-discount subtotal. Zero for an untaxed event.</param>
/// <param name="TotalMinor">What the buyer would pay.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="TaxLabel">Display name for the tax (e.g. <c>"GST 18%"</c>), when taxed.</param>
/// <param name="PromoCodeApplied">The code that was accepted, or <see langword="null"/>.</param>
/// <param name="PromoCodeRejection">
/// Why a supplied code was not accepted, as a machine-readable reason
/// (<c>NotFound</c>, <c>Expired</c>, <c>RedemptionLimitReached</c>, …), or <see langword="null"/>.
/// A rejected code is **not** an error response — the quote is still valid, just undiscounted, so
/// the buyer sees the real total alongside the reason their code didn't work.
/// </param>
public sealed record CheckoutQuoteResponse(
    long SubtotalMinor,
    long DiscountMinor,
    long TaxMinor,
    long TotalMinor,
    string Currency,
    string? TaxLabel,
    string? PromoCodeApplied,
    string? PromoCodeRejection);
