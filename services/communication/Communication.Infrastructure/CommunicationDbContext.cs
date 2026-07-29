namespace Communication.Infrastructure;

/// <summary>
/// EF Core database context for the Communication service (schema <c>communication</c>). Unlike
/// every other service's context, this does not implement an outbox contract — Communication never
/// publishes an integration event, so it has nothing to relay.
/// </summary>
/// <param name="options">The context options.</param>
public sealed class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : DbContext(options)
{
    /// <summary>The delivery-log table — one row per attempted send.</summary>
    public DbSet<DeliveryLogEntry> DeliveryLog => Set<DeliveryLogEntry>();

    /// <summary>The inbound-event dedup ledger.</summary>
    internal DbSet<ProcessedNotificationEvent> ProcessedNotificationEvents => Set<ProcessedNotificationEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("communication");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
