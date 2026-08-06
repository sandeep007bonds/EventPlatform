namespace Ticketing.Infrastructure;

/// <summary>EF Core implementation of <see cref="IEventScanContextRepository"/>.</summary>
/// <param name="dbContext">The Ticketing database context.</param>
internal sealed class EventScanContextRepository(TicketingDbContext dbContext) : IEventScanContextRepository
{
    /// <inheritdoc />
    public Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.EventScanContexts.AnyAsync(c => c.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public void AddContext(EventScanContext context) => dbContext.EventScanContexts.Add(context);

    /// <inheritdoc />
    public void AddSeatGates(IEnumerable<SeatEntryGate> assignments) => dbContext.SeatEntryGates.AddRange(assignments);

    /// <inheritdoc />
    public void AddGaAllocationGates(IEnumerable<GaAllocationGate> assignments) => dbContext.GaAllocationGates.AddRange(assignments);

    /// <inheritdoc />
    public Task<EventScanContext?> GetContextAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.EventScanContexts.AsNoTracking().FirstOrDefaultAsync(c => c.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public async Task<Guid?> GetGateForSeatAsync(Guid seatId, CancellationToken cancellationToken)
    {
        var gate = await dbContext.SeatEntryGates.AsNoTracking().FirstOrDefaultAsync(g => g.SeatId == seatId, cancellationToken);
        return gate?.EntryGateId;
    }

    /// <inheritdoc />
    public async Task<Guid?> GetGateForGaAllocationAsync(Guid allocationId, CancellationToken cancellationToken)
    {
        var gate = await dbContext.GaAllocationGates.AsNoTracking().FirstOrDefaultAsync(g => g.AllocationId == allocationId, cancellationToken);
        return gate?.EntryGateId;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
