namespace Ordering.Api.Endpoints;

/// <summary>Read model returned for a single order.</summary>
/// <param name="Id">Order id.</param>
/// <param name="Status">Order status name.</param>
/// <param name="TotalMinor">What the buyer paid, in minor currency units: subtotal − discount + tax.</param>
/// <param name="SubtotalMinor">Sum of the line prices, before discount or tax.</param>
/// <param name="DiscountMinor">What a promo code took off. Zero when none was applied.</param>
/// <param name="TaxMinor">Tax charged on the post-discount subtotal. Zero for an untaxed event.</param>
/// <param name="TaxLabel">The tax's display name at time of purchase (e.g. <c>"GST 18%"</c>).</param>
/// <param name="PromoCode">The discount code redeemed, if any.</param>
/// <param name="Currency">Pricing currency (ISO 4217).</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="HoldId">The hold the order was created from.</param>
/// <param name="Lines">The order lines.</param>
/// <param name="PaymentClientSecret">
/// The Stripe PaymentIntent client secret, while the order is awaiting payment — lets a buyer who
/// reloads mid-authentication (or a redirect-return page) resume Payment Element without a fresh
/// checkout call. <see langword="null"/> once the order reaches a terminal status.
/// </param>
public sealed record OrderResponse(
    Guid Id,
    string Status,
    long TotalMinor,
    long SubtotalMinor,
    long DiscountMinor,
    long TaxMinor,
    string? TaxLabel,
    string? PromoCode,
    string Currency,
    Guid CatalogEventId,
    Guid HoldId,
    IReadOnlyList<OrderLineResponse> Lines,
    string? PaymentClientSecret);
