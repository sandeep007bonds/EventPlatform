namespace Payments.Infrastructure;

/// <summary>EF Core database context for the Payments service (schema <c>payments</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The payments table.</summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        base.OnModelCreating(modelBuilder);
    }
}
