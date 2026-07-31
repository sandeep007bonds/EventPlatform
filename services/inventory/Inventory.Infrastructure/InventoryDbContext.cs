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

    /// <summary>The per-event settings table (currently just the enforced booking cutoff).</summary>
    public DbSet<EventInventorySettings> EventInventorySettings => Set<EventInventorySettings>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        base.OnModelCreating(modelBuilder);
    }
}
