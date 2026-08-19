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

    /// <summary>Gets an order (with lines) by id, or <see langword="null"/>. Tracked — use for writes.</summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The order, or <see langword="null"/>.</returns>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Reads an order by id **untracked**, so repeated calls on the same scope always observe the
    /// current database state. Required by any read-only poll that watches for a change another
    /// process is making concurrently (e.g. checkout waiting for the saga to record the payment
    /// client secret) — <see cref="GetByIdAsync"/>'s tracking identity map would otherwise keep
    /// returning the first-loaded instance with stale values for the life of the scope.
    /// </summary>
    /// <param name="id">The order id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The order, or <see langword="null"/>.</returns>
    Task<Order?> GetSnapshotByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Gets an order by its buyer-scoped idempotency key (checkout dedupe).</summary>
    /// <param name="userId">The buyer.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching order, or <see langword="null"/>.</returns>
    Task<Order?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// Counts how many orders have redeemed a promo code, optionally narrowed to one buyer —
    /// the two numbers a code's <c>MaxRedemptions</c> and <c>MaxRedemptionsPerBuyer</c> caps are
    /// checked against.
    /// </summary>
    /// <remarks>
    /// <c>Failed</c> and <c>Refunded</c> orders are excluded. A checkout that never completed did
    /// not consume a redemption, and counting it would permanently burn allowance every time a
    /// buyer abandoned a payment; a refunded order was unwound entirely — its seats went back to
    /// the pool, so its discount slot should too, or a "first 100 orders" promotion would quietly
    /// serve fewer than 100 people.
    /// <para>
    /// Orders still <c>Pending</c>/<c>AwaitingPayment</c> DO count — otherwise two buyers racing
    /// the last redemption would both pass the check, which is the exact thing the cap exists to
    /// prevent.
    /// </para>
    /// </remarks>
    /// <param name="promoCodeId">The Catalog promo-code id.</param>
    /// <param name="userId">Narrow to this buyer, or <see langword="null"/> to count every buyer.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of non-failed orders carrying that code.</returns>
    Task<int> CountPromoRedemptionsAsync(Guid promoCodeId, Guid? userId, CancellationToken cancellationToken);

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
