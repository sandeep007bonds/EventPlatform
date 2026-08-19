namespace Ordering.Api.Endpoints;

/// <summary>
/// Request body for a checkout price quote — what a purchase would cost, without creating anything.
/// </summary>
/// <param name="HoldId">The hold being priced.</param>
/// <param name="PromoCode">A discount code to try, or <see langword="null"/> to price without one.</param>
public sealed record CheckoutQuoteRequest(Guid HoldId, string? PromoCode = null);
