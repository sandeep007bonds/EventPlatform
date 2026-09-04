namespace Ticketing.Infrastructure;

/// <summary>EF Core database context for the Ticketing service (schema <c>ticketing</c>).</summary>
/// <param name="options">The context options.</param>
public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options)
    : DbContext(options), IOutboxDbContext, IDeadLetterDbContext
{
    /// <inheritdoc />
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();

    /// <summary>The tickets table.</summary>
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <summary>Per-event check-in window settings, learned once from <c>EventPublished</c>.</summary>
    public DbSet<SessionScanContext> SessionScanContexts => Set<SessionScanContext>();

    /// <summary>Reserved-seat-to-entry-gate assignments, resolved once from Catalog's seat map.</summary>
    public DbSet<SeatEntryGate> SeatEntryGates => Set<SeatEntryGate>();

    /// <summary>General-admission-allocation-to-entry-gate assignments, resolved once.</summary>
    public DbSet<GaAllocationGate> GaAllocationGates => Set<GaAllocationGate>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema("ticketing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
        modelBuilder.ApplyOutbox();

        modelBuilder.ApplyDeadLetters();

        // Audit shadow properties, last so every configuration and the outbox mapping are
        // already in the model (ADR-0036).
        modelBuilder.ApplyAuditFields();

        base.OnModelCreating(modelBuilder);
    }
}
