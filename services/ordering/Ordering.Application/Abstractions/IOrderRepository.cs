namespace Ordering.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Order"/> aggregate. Implemented in the Infrastructure
/// layer so the Application layer stays free of EF Core.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Adds and persists a new order, tolerating a concurrent duplicate. Returns
    /// <see langword="false"/> when another order with the same tenant-scoped idempotency key
    /// already exists (the unique index rejected the insert) — the caller should re-fetch the
    /// winner rather than treat it as an error. Any other failure still throws.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if this call persisted the order; <see langword="false"/> if a
    /// concurrent duplicate already claimed the idempotency key.</returns>
    Task<bool> TryAddAsync(Order order, CancellationToken cancellationToken);

    /// <summary>Gets an order (with lines) by id, or <see langword="null"/>.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The order, or <see langword="null"/>.</returns>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Gets an order by its tenant-scoped idempotency key (checkout dedupe).</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching order, or <see langword="null"/>.</returns>
    Task<Order?> GetByIdempotencyKeyAsync(Guid tenantId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
