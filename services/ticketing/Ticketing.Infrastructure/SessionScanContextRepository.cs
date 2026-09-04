namespace Ticketing.Infrastructure;

/// <summary>EF Core implementation of <see cref="ISessionScanContextRepository"/>.</summary>
/// <param name="dbContext">The Ticketing database context.</param>
internal sealed class SessionScanContextRepository(TicketingDbContext dbContext) : ISessionScanContextRepository
{
    /// <inheritdoc />
    public Task<bool> ExistsForSessionAsync(Guid eventSessionId, CancellationToken cancellationToken) =>
        dbContext.SessionScanContexts.AnyAsync(c => c.EventSessionId == eventSessionId, cancellationToken);

    /// <inheritdoc />
    public void AddContext(SessionScanContext context) => dbContext.SessionScanContexts.Add(context);

    /// <inheritdoc />
    public void AddSeatGates(IEnumerable<SeatEntryGate> assignments) => dbContext.SeatEntryGates.AddRange(assignments);

    /// <inheritdoc />
    public void AddGaAllocationGates(IEnumerable<GaAllocationGate> assignments) => dbContext.GaAllocationGates.AddRange(assignments);

    /// <inheritdoc />
    public Task<SessionScanContext?> GetContextAsync(Guid eventSessionId, CancellationToken cancellationToken) =>
        dbContext.SessionScanContexts.AsNoTracking().FirstOrDefaultAsync(c => c.EventSessionId == eventSessionId, cancellationToken);

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
