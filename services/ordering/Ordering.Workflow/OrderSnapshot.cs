namespace Ordering.Workflow;

/// <summary>An order as read from Ordering's own database, for the cancellation saga.</summary>
/// <param name="OrderId">The order id.</param>
/// <param name="UserId">The buyer who owns the order.</param>
/// <param name="HoldId">The hold the order was created from.</param>
/// <param name="Status">Order status name (<c>Pending</c>, <c>AwaitingPayment</c>, <c>Confirmed</c>, <c>Failed</c>, <c>Refunded</c>).</param>
/// <param name="IdempotencyKey">The order's idempotency key, reused as the refund's idempotency key.</param>
public sealed record OrderSnapshot(Guid OrderId, Guid UserId, Guid HoldId, string Status, string IdempotencyKey);
