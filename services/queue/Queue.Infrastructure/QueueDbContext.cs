namespace Queue.Infrastructure;

/// <summary>
/// EF Core database context for the Queue service (schema <c>queue</c>). No outbox — Queue never
/// publishes an integration event, same posture as Communication/Identity.
/// </summary>
/// <param name="options">The context options.</param>
public sealed class QueueDbContext(DbContextOptions<QueueDbContext> options) : DbContext(options)
{
    /// <summary>Per-event waiting-room configuration.</summary>
    public DbSet<QueueSettings> QueueSettings => Set<QueueSettings>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("queue");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QueueDbContext).Assembly);

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
