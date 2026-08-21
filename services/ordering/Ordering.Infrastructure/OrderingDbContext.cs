namespace Ordering.Infrastructure;

/// <summary>EF Core database context for the Ordering service (schema <c>ordering</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The orders table.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
