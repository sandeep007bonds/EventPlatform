namespace Ordering.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Order"/> aggregate. Implemented in the Infrastructure
/// layer so the Application layer stays free of EF Core.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Adds and persists a new order, tolerating a concurrent duplicate. Returns
    /// <see langword="false"/> when another order with the same buyer-scoped idempotency key
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

    /// <summary>Gets an order by its buyer-scoped idempotency key (checkout dedupe).</summary>
    /// <param name="userId">The buyer.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching order, or <see langword="null"/>.</returns>
    Task<Order?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Lists orders, filtered by tenant and/or buyer. At least one filter is expected to be
    /// non-null — the caller (endpoint layer) enforces that a caller cannot list every order
    /// platform-wide.
    /// </summary>
    /// <param name="tenantId">Restrict to this tenant's orders, or <see langword="null"/> to skip the filter.</param>
    /// <param name="userId">Restrict to this buyer's orders, or <see langword="null"/> to skip the filter.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The page of orders (without lines) and the total count matching the filter.</returns>
    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        Guid? tenantId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
