namespace Ticketing.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Ticket"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface ITicketRepository
{
    /// <summary>Registers new tickets to be persisted.</summary>
    /// <param name="tickets">The tickets to add.</param>
    void AddRange(IEnumerable<Ticket> tickets);

    /// <summary>Returns whether tickets already exist for an order (issuance dedupe).</summary>
    /// <param name="orderId">The order id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the order already has tickets.</returns>
    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Gets the tickets for an order.</summary>
    /// <param name="orderId">The order id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The order's tickets.</returns>
    Task<IReadOnlyList<Ticket>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>Gets a ticket by id, or <see langword="null"/>.</summary>
    /// <param name="id">The ticket id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The ticket, or <see langword="null"/>.</returns>
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
