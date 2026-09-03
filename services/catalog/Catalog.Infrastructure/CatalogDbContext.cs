namespace Catalog.Infrastructure;

/// <summary>EF Core database context for the Catalog service (schema <c>catalog</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The events table.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>The performances table — the grain every downstream service keys on.</summary>
    public DbSet<EventSession> EventSessions => Set<EventSession>();

    /// <summary>The event groups (tours) table.</summary>
    public DbSet<EventGroup> EventGroups => Set<EventGroup>();

    /// <summary>Organizer-created discount codes.</summary>
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    /// <summary>The named, priced ticket types a performance's blocks are sold as.</summary>
    public DbSet<TicketType> TicketTypes => Set<TicketType>();

    /// <summary>Organizer terms, privacy and refund documents — tenant defaults and event overrides.</summary>
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
