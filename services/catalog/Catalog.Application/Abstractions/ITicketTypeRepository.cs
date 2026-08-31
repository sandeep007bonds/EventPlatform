namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="TicketType"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface ITicketTypeRepository
{
    /// <summary>Registers a new ticket type to be persisted.</summary>
    /// <param name="ticketType">The ticket type to add.</param>
    void Add(TicketType ticketType);

    /// <summary>Gets a ticket type by id, tracked for update, or <see langword="null"/>.</summary>
    /// <param name="id">The ticket-type id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The ticket type, or <see langword="null"/>.</returns>
    Task<TicketType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Finds an event's ticket type by name, compared case-insensitively.
    /// </summary>
    /// <remarks>
    /// This is what enforces the one-name-per-event rule: the unique index is exact-match, because
    /// the name is displayed as typed, so the case-insensitive half of the invariant lives here and
    /// in the handlers that call this before writing.
    /// </remarks>
    /// <param name="eventId">The event the type belongs to.</param>
    /// <param name="name">The name, in any case.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The ticket type, or <see langword="null"/> if the event has no type by that name.</returns>
    Task<TicketType?> GetByNameAsync(Guid eventId, string name, CancellationToken cancellationToken);

    /// <summary>Lists every ticket type defined for an event, active or not — the organizer's view.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event's ticket types, in display order.</returns>
    Task<IReadOnlyList<TicketType>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Persists pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
