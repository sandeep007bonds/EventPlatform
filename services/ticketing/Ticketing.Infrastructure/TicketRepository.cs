namespace Ticketing.Infrastructure;

/// <summary>EF Core implementation of <see cref="ITicketRepository"/>.</summary>
/// <param name="dbContext">The Ticketing database context.</param>
internal sealed class TicketRepository(TicketingDbContext dbContext) : ITicketRepository
{
    /// <inheritdoc />
    public void AddRange(IEnumerable<Ticket> tickets) => dbContext.Tickets.AddRange(tickets);

    /// <inheritdoc />
    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Tickets.AnyAsync(t => t.OrderId == orderId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.OrderId == orderId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Ticket?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        dbContext.Tickets.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
