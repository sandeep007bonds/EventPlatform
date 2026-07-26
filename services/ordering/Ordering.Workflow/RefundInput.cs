namespace Ordering.Workflow;

/// <summary>Input to the refund activity (compensation).</summary>
/// <param name="OrderId">The order to refund.</param>
/// <param name="IdempotencyKey">Idempotency key.</param>
public sealed record RefundInput(Guid OrderId, string IdempotencyKey);
