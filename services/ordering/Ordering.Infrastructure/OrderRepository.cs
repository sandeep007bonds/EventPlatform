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
    public Task<Order?> GetSnapshotByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Order?> GetByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        dbContext.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(
                o => o.UserId == userId && o.IdempotencyKey == idempotencyKey,
                cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        Guid? tenantId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders.AsNoTracking().AsQueryable();

        if (tenantId is not null)
        {
            query = query.Where(o => o.TenantId == tenantId);
        }

        if (userId is not null)
        {
            query = query.Where(o => o.UserId == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
