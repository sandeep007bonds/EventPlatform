namespace Ordering.Workflow;

/// <summary>Input to the refund activity (compensation).</summary>
/// <param name="OrderId">The order to refund.</param>
/// <param name="IdempotencyKey">Idempotency key.</param>
/// <param name="AmountMinor">
/// How much to return, in minor units, or <see langword="null"/> for everything captured. A
/// cancelled sale returns all but the non-refundable booking fee; a checkout that never completed
/// returns the lot, because the buyer received nothing to charge a fee for.
/// </param>
public sealed record RefundInput(Guid OrderId, string IdempotencyKey, long? AmountMinor = null);
