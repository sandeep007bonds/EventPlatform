namespace Ordering.Workflow;

/// <summary>Output of the create-order activity.</summary>
/// <param name="OrderId">The created (or already-existing) order id.</param>
/// <param name="TotalMinor">Order total in minor units.</param>
/// <param name="Currency">Order currency (ISO 4217).</param>
/// <param name="AlreadyExisted">
/// <see langword="true"/> when a concurrent checkout for the same idempotency key had already
/// created the order — this attempt must not charge again.
/// </param>
public sealed record CreateOrderOutput(Guid OrderId, long TotalMinor, string Currency, bool AlreadyExisted);
