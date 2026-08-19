namespace Ordering.Workflow;

/// <summary>Input to the checkout workflow.</summary>
/// <param name="UserId">The buyer.</param>
/// <param name="HoldId">The hold to purchase.</param>
/// <param name="IdempotencyKey">Idempotency key (unique per buyer).</param>
/// <param name="BuyerEmail">The buyer's email, for ticket delivery.</param>
/// <param name="OrderId">
/// The order's id, pre-minted by the checkout endpoint before scheduling this workflow — also used
/// as this workflow's own Dapr instance id, so a payment webhook can raise an event straight back to
/// the running saga with no lookup table.
/// </param>
/// <param name="PromoCode">
/// The discount code the buyer applied, or <see langword="null"/> for none. Re-validated inside the
/// saga — the quote the buyer saw is advisory, and a code can expire or run out in between.
/// </param>
public sealed record CheckoutWorkflowInput(
    Guid UserId,
    Guid HoldId,
    string IdempotencyKey,
    string BuyerEmail,
    Guid OrderId,
    string? PromoCode = null);
