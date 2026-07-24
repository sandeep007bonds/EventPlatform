namespace Catalog.Infrastructure;

/// <summary>EF Core database context for the Catalog service (schema <c>catalog</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The events table.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        base.OnModelCreating(modelBuilder);
    }
}
