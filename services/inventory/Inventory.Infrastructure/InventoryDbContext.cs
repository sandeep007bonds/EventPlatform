namespace Inventory.Infrastructure;

/// <summary>EF Core database context for the Inventory service (schema <c>inventory</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The inventory items table (system of record for availability).</summary>
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    /// <summary>The holds table.</summary>
    public DbSet<Hold> Holds => Set<Hold>();

    /// <summary>The append-only inventory ledger.</summary>
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    /// <summary>The general-admission capacity pools table.</summary>
    public DbSet<GeneralAdmissionAllocation> GeneralAdmissionAllocations => Set<GeneralAdmissionAllocation>();

    /// <summary>The per-performance settings table — selling window, buyer limit, queue, pause.</summary>
    public DbSet<SessionInventorySettings> SessionInventorySettings => Set<SessionInventorySettings>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
