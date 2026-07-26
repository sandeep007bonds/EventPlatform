namespace Payments.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Payment"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>Registers a new payment to be persisted.</summary>
    /// <param name="payment">The payment to add.</param>
    void Add(Payment payment);

    /// <summary>Gets a payment by order and idempotency key (charge dedupe).</summary>
    /// <param name="orderId">The order id.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching payment, or <see langword="null"/>.</returns>
    Task<Payment?> GetByOrderAndKeyAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Gets the captured payment for an order, if any (for refunds).</summary>
    /// <param name="orderId">The order id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The captured payment, or <see langword="null"/>.</returns>
    Task<Payment?> GetCapturedByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Gets the payment with the given provider reference, if any (webhook correlation).</summary>
    /// <param name="providerReference">The PSP reference (e.g. Stripe PaymentIntent id).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching payment, or <see langword="null"/>.</returns>
    Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken);

    /// <summary>Checks whether a provider webhook event has already been processed (dedupe).</summary>
    /// <param name="eventId">The provider's unique event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the event was processed before.</returns>
    Task<bool> HasProcessedWebhookAsync(string eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Records a provider webhook event as processed, in the same unit of work as the payment change
    /// it drove, so a redelivery is a no-op (exactly-once processing).
    /// </summary>
    /// <param name="eventId">The provider's unique event id.</param>
    /// <param name="eventType">The raw provider event type (diagnostics).</param>
    void RecordProcessedWebhook(string eventId, string eventType);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
