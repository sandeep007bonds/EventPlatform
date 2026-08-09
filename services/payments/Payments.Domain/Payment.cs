namespace Payments.Domain;

/// <summary>
/// A payment against an order. Deduped by <see cref="IdempotencyKey"/> per order so a retried
/// charge never double-charges. Provider details (e.g. Stripe PaymentIntent) are held opaquely.
/// </summary>
public sealed class Payment
{
    // Parameterless ctor for EF Core materialization.
    private Payment()
    {
    }

    private Payment(
        Guid id,
        Guid tenantId,
        Guid orderId,
        string provider,
        string idempotencyKey,
        long amountMinor,
        string currency)
    {
        Id = id;
        TenantId = tenantId;
        OrderId = orderId;
        Provider = provider;
        IdempotencyKey = idempotencyKey;
        AmountMinor = amountMinor;
        Currency = currency;
        Status = PaymentStatus.Initiated;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Unique payment id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The order being paid.</summary>
    public Guid OrderId { get; private set; }

    /// <summary>Payment provider (e.g. <c>stripe-test</c>).</summary>
    public string Provider { get; private set; } = default!;

    /// <summary>Provider reference (e.g. Stripe PaymentIntent id), once known.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>
    /// The provider's client secret (e.g. Stripe's PaymentIntent client secret), once known — used
    /// by the frontend to mount Stripe's Payment Element and confirm the payment. Never re-derivable
    /// from <see cref="ProviderReference"/> alone, so it is stored, not recomputed.
    /// </summary>
    public string? ClientSecret { get; private set; }

    /// <summary>Idempotency key; unique per order, dedupes retried charges.</summary>
    public string IdempotencyKey { get; private set; } = default!;

    /// <summary>Current payment status.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Amount in minor currency units.</summary>
    public long AmountMinor { get; private set; }

    /// <summary>Pricing currency (ISO 4217).</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>Reason the charge failed, when <see cref="Status"/> is <see cref="PaymentStatus.Failed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>When the payment was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates an initiated payment for an order.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The order being paid.</param>
    /// <param name="provider">Payment provider name.</param>
    /// <param name="idempotencyKey">Idempotency key (unique per order).</param>
    /// <param name="amountMinor">Amount in minor units (non-negative).</param>
    /// <param name="currency">ISO 4217 currency code.</param>
    /// <returns>A new initiated <see cref="Payment"/>.</returns>
    public static Payment Create(
        Guid tenantId,
        Guid orderId,
        string provider,
        string idempotencyKey,
        long amountMinor,
        string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentOutOfRangeException.ThrowIfNegative(amountMinor);

        return new Payment(Guid.CreateVersion7(), tenantId, orderId, provider, idempotencyKey, amountMinor, currency);
    }

    /// <summary>Marks the charge captured with its provider reference.</summary>
    /// <param name="providerReference">The PSP reference.</param>
    /// <exception cref="InvalidOperationException">The payment is not initiated.</exception>
    public void MarkCaptured(string providerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);
        Require(PaymentStatus.Initiated);

        ProviderReference = providerReference;
        Status = PaymentStatus.Captured;
    }

    /// <summary>Marks the charge failed with a reason.</summary>
    /// <param name="reason">Why the charge failed.</param>
    /// <exception cref="InvalidOperationException">The payment is not initiated.</exception>
    public void MarkFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Require(PaymentStatus.Initiated);

        FailureReason = reason;
        Status = PaymentStatus.Failed;
    }

    /// <summary>Marks a captured payment refunded.</summary>
    /// <exception cref="InvalidOperationException">The payment is not captured.</exception>
    public void MarkRefunded()
    {
        Require(PaymentStatus.Captured);
        Status = PaymentStatus.Refunded;
    }

    /// <summary>
    /// Records the provider reference and client secret for the just-created intent, while the
    /// payment is still in flight — so an out-of-band provider webhook can correlate back to this
    /// payment, and the frontend can mount Stripe's Payment Element. Setting the same reference again
    /// is a no-op (an idempotent retry of the same create call).
    /// </summary>
    /// <param name="providerReference">The PSP reference.</param>
    /// <param name="clientSecret">The PSP client secret.</param>
    /// <exception cref="InvalidOperationException">The payment is no longer initiated.</exception>
    public void RecordIntentDetails(string providerReference, string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        if (string.Equals(ProviderReference, providerReference, StringComparison.Ordinal))
        {
            return;
        }

        Require(PaymentStatus.Initiated);
        ProviderReference = providerReference;
        ClientSecret = clientSecret;
    }

    /// <summary>
    /// Idempotently captures the payment in response to a provider notification. Returns
    /// <see langword="false"/> without changing state when the payment is not initiated (the
    /// notification was a duplicate or arrived after another transition).
    /// </summary>
    /// <param name="providerReference">The PSP reference.</param>
    /// <returns><see langword="true"/> if this call captured the payment.</returns>
    public bool TryMarkCaptured(string providerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);
        if (Status != PaymentStatus.Initiated)
        {
            return false;
        }

        ProviderReference = providerReference;
        Status = PaymentStatus.Captured;
        return true;
    }

    /// <summary>
    /// Idempotently fails the payment in response to a provider notification. Returns
    /// <see langword="false"/> without changing state when the payment is not initiated.
    /// </summary>
    /// <param name="reason">Why the charge failed.</param>
    /// <returns><see langword="true"/> if this call failed the payment.</returns>
    public bool TryMarkFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status != PaymentStatus.Initiated)
        {
            return false;
        }

        FailureReason = reason;
        Status = PaymentStatus.Failed;
        return true;
    }

    /// <summary>
    /// Idempotently refunds the payment in response to a provider notification. Returns
    /// <see langword="false"/> without changing state when the payment is not captured.
    /// </summary>
    /// <returns><see langword="true"/> if this call refunded the payment.</returns>
    public bool TryMarkRefunded()
    {
        if (Status != PaymentStatus.Captured)
        {
            return false;
        }

        Status = PaymentStatus.Refunded;
        return true;
    }

    private void Require(PaymentStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Payment {Id} is {Status}, expected {expected}.");
        }
    }
}
