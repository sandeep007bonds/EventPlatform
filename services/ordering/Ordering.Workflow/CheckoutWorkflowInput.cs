namespace Ordering.Workflow;

/// <summary>Input to the checkout workflow.</summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="UserId">The buyer.</param>
/// <param name="HoldId">The hold to purchase.</param>
/// <param name="IdempotencyKey">Idempotency key (unique per tenant).</param>
public sealed record CheckoutWorkflowInput(Guid TenantId, Guid UserId, Guid HoldId, string IdempotencyKey);
