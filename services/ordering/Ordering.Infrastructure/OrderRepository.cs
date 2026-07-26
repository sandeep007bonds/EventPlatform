namespace Ordering.Infrastructure;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
/// <param name="dbContext">The Ordering database context.</param>
internal sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    /// <inheritdoc />
    public async Task<bool> TryAddAsync(Order order, CancellationToken cancellationToken)
    {
        dbContext.Orders.Add(order);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // A concurrent checkout with the same idempotency key won the race. Drop the rejected
            // order graph (root + lines) so the context is clean, and let the caller re-fetch.
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    /// <inheritdoc />
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Order?> GetByIdempotencyKeyAsync(
        Guid tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(
                o => o.TenantId == tenantId && o.IdempotencyKey == idempotencyKey,
                cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
