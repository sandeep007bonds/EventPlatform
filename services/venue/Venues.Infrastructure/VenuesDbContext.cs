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

        // Every id here is minted by the domain, so EF must not read "the key is already set" as
        // "this row exists" — that is what made an edit to a stored seat map issue an UPDATE
        // against a brand-new id and fail with a concurrency error.
        modelBuilder.ApplyClientGeneratedKeys();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
