namespace Payments.Api.Endpoints;

/// <summary>Request body for charging an order (called by the checkout saga).</summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="OrderId">The order being paid.</param>
/// <param name="AmountMinor">Amount in minor currency units.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="IdempotencyKey">Idempotency key (unique per order).</param>
/// <param name="PaymentMethodId">The Stripe payment-method id tokenized client-side (PCI SAQ-A).</param>
public sealed record ChargeRequest(
    Guid TenantId,
    Guid OrderId,
    long AmountMinor,
    string Currency,
    string IdempotencyKey,
    string PaymentMethodId);
