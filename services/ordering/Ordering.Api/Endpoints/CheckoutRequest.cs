namespace Ordering.Api.Endpoints;

/// <summary>
/// Request body for checkout. The tenant and user come from the caller's token; the idempotency
/// key comes from the <c>Idempotency-Key</c> header (ADR-0011).
/// </summary>
/// <param name="HoldId">The hold to purchase.</param>
public sealed record CheckoutRequest(Guid HoldId);
