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

    /// <summary>The processed provider-webhook idempotency ledger (infrastructure-internal).</summary>
    internal DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
