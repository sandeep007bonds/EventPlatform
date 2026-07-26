namespace Ordering.Workflow;

/// <summary>Input to the charge activity.</summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="OrderId">The order being paid.</param>
/// <param name="AmountMinor">Amount in minor units.</param>
/// <param name="Currency">Currency (ISO 4217).</param>
/// <param name="IdempotencyKey">Idempotency key.</param>
public sealed record ChargeInput(Guid TenantId, Guid OrderId, long AmountMinor, string Currency, string IdempotencyKey);
