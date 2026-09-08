namespace Venues.Application.Abstractions;

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
    /// Gets a seat map with every version's full layout loaded, change-tracked so edits are saved.
    /// </summary>
    /// <param name="id">The seat-map id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked seat map, or <see langword="null"/>.</returns>
    Task<SeatMap?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a seat map with a single version's full layout loaded, for reading.
    /// </summary>
    /// <param name="id">The seat-map id.</param>
    /// <param name="versionNumber">
    /// The version to load, or <see langword="null"/> for whichever is published.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The seat map, or <see langword="null"/> if the map or that version does not exist.</returns>
    Task<SeatMap?> GetWithVersionAsync(Guid id, int? versionNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Lists a venue's seat maps without their layouts — enough to choose between configurations,
    /// not enough to draw one.
    /// </summary>
    /// <param name="venueId">The venue id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The venue's seat maps.</returns>
    Task<IReadOnlyList<SeatMap>> ListForVenueAsync(Guid venueId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists all pending changes, returning <see langword="false"/> instead of throwing when the
    /// rows this save expected to change are not the rows the database still holds.
    /// </summary>
    /// <remarks>
    /// Replacing a draft's layout deletes every section, row and seat it had and writes the new
    /// ones. Two saves of the same draft in flight together therefore both try to delete the same
    /// rows, and the second finds them gone — which EF reports as a concurrency conflict. It is a
    /// lost race, not a server fault, so the caller turns it into an answer the organizer can act
    /// on rather than a 500.
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if saved; <see langword="false"/> on a concurrency conflict.</returns>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}
