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
public sealed record CreateOrderInput(
    Guid TenantId,
    Guid UserId,
    Guid HoldId,
    string IdempotencyKey,
    Guid CatalogEventId,
    IReadOnlyList<HoldLineSnapshot> Lines,
    string BuyerEmail,
    Guid OrderId);
