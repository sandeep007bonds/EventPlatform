namespace Venues.Infrastructure;

/// <summary>EF Core database context for the Venue service (schema <c>venue</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class VenuesDbContext(DbContextOptions<VenuesDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The venues table.</summary>
    public DbSet<Venue> Venues => Set<Venue>();

    /// <summary>The seat maps table — one row per seating configuration, not per version.</summary>
    public DbSet<SeatMap> SeatMaps => Set<SeatMap>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("venue");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VenuesDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
