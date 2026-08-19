namespace Ordering.Api.Endpoints;

/// <summary>
/// Request body for checkout. The tenant and user come from the caller's token; the idempotency
/// key comes from the <c>Idempotency-Key</c> header (ADR-0011).
/// </summary>
/// <param name="HoldId">The hold to purchase.</param>
/// <param name="BuyerEmail">
/// The buyer's email, for ticket delivery — required so Ticketing/Communication can send a single
/// combined ticket email once the order is confirmed. Not derived from any token claim.
/// </param>
/// <param name="PromoCode">
/// A discount code to apply, or <see langword="null"/> for none. Re-validated server-side inside
/// the saga — whatever the buyer was quoted is advisory, since a code can expire or hit its
/// redemption cap between the preview and this call.
/// </param>
public sealed record CheckoutRequest(Guid HoldId, string BuyerEmail, string? PromoCode = null);
