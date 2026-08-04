namespace Ordering.Workflow;

/// <summary>Input to the checkout workflow.</summary>
/// <param name="UserId">The buyer.</param>
/// <param name="HoldId">The hold to purchase.</param>
/// <param name="IdempotencyKey">Idempotency key (unique per buyer).</param>
/// <param name="BuyerEmail">The buyer's email, for ticket delivery.</param>
public sealed record CheckoutWorkflowInput(Guid UserId, Guid HoldId, string IdempotencyKey, string BuyerEmail);
