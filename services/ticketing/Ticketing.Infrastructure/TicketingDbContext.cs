namespace Ticketing.Infrastructure;

/// <summary>EF Core database context for the Ticketing service (schema <c>ticketing</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    /// <summary>The tickets table.</summary>
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("ticketing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        base.OnModelCreating(modelBuilder);
    }
}
