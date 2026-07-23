using Catalog.Domain;

namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Event"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface IEventRepository
{
    /// <summary>Registers a new event to be persisted.</summary>
    /// <param name="event">The event to add.</param>
    void Add(Event @event);

    /// <summary>Gets an event by id, or <see langword="null"/> if it does not exist.</summary>
    /// <param name="id">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event, or <see langword="null"/>.</returns>
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
