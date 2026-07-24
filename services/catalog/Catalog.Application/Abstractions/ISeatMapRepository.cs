namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="SeatMap"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface ISeatMapRepository
{
    /// <summary>Registers a new seat map to be persisted.</summary>
    /// <param name="seatMap">The seat map to add.</param>
    void Add(SeatMap seatMap);

    /// <summary>
    /// Gets the seat map for an event (with its seats), or <see langword="null"/> if none exists.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The seat map, or <see langword="null"/>.</returns>
    Task<SeatMap?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
