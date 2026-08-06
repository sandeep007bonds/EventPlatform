namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="EntryGate"/> entity. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface IEntryGateRepository
{
    /// <summary>Registers a new entry gate to be persisted.</summary>
    /// <param name="entryGate">The entry gate to add.</param>
    void Add(EntryGate entryGate);

    /// <summary>Gets an entry gate by id, or <see langword="null"/> if it does not exist.</summary>
    /// <param name="id">The entry-gate id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entry gate, or <see langword="null"/>.</returns>
    Task<EntryGate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists every entry gate defined for an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event's entry gates.</returns>
    Task<IReadOnlyList<EntryGate>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
