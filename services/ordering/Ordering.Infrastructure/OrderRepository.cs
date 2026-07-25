namespace Ordering.Infrastructure;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
/// <param name="dbContext">The Ordering database context.</param>
internal sealed class OrderRepository(OrderingDbContext dbContext) : IOrderRepository
{
    /// <inheritdoc />
    public void Add(Order order) => dbContext.Orders.Add(order);

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
