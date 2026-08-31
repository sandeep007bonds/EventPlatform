namespace Ordering.Workflow;

/// <summary>Input to the create-order activity.</summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="UserId">The buyer.</param>
/// <param name="HoldId">The hold being purchased.</param>
/// <param name="IdempotencyKey">Idempotency key.</param>
/// <param name="CatalogEventId">The show/event.</param>
/// <param name="Lines">The held seats and their prices.</param>
/// <param name="BuyerEmail">The buyer's email, for ticket delivery.</param>
/// <param name="OrderId">
/// The order's id, pre-minted by the checkout endpoint (also the workflow's own instance id) — used
/// as <see cref="Order.Id"/> when a new order is actually created; unused on the already-existed
/// fast path, which returns the winner's own real id instead.
/// </param>
/// <param name="Currency">
/// ISO 4217 currency the order is priced in, read from the Catalog event (falling back to
/// <c>CheckoutOptions.DefaultCurrency</c> when Catalog can't be reached).
/// </param>
/// <param name="PromoTerms">The accepted promo code's arithmetic terms, or <see langword="null"/> for none.</param>
/// <param name="PromoCodeId">The accepted code's Catalog id, for counting redemptions.</param>
/// <param name="PromoCodeText">The accepted code as redeemed, for display on the order.</param>
/// <param name="TaxRatePercent">The event's tax rate as a percentage, or <see langword="null"/> when untaxed.</param>
/// <param name="TaxLabel">The tax's display name (e.g. <c>"GST 18%"</c>).</param>
/// <param name="BookingFeePerTicketMinor">The event's per-ticket booking fee in minor units; 0 when it charges none.</param>
public sealed record CreateOrderInput(
    Guid TenantId,
    Guid UserId,
    Guid HoldId,
    string IdempotencyKey,
    Guid CatalogEventId,
    IReadOnlyList<HoldLineSnapshot> Lines,
    string BuyerEmail,
    Guid OrderId,
    string Currency,
    PromoCodeTerms? PromoTerms = null,
    Guid? PromoCodeId = null,
    string? PromoCodeText = null,
    decimal? TaxRatePercent = null,
    string? TaxLabel = null,
    long BookingFeePerTicketMinor = 0);
