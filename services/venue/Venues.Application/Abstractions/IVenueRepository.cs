namespace Venues.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Venue"/> aggregate. Implemented in the Infrastructure
/// layer so the Application layer stays free of EF Core.
/// </summary>
public interface IVenueRepository
{
    /// <summary>Registers a new venue to be persisted.</summary>
    /// <param name="venue">The venue to add.</param>
    void Add(Venue venue);

    /// <summary>
    /// Gets a venue with its gates and facilities for modification, or <see langword="null"/> if it
    /// does not exist.
    /// </summary>
    /// <param name="id">The venue id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tracked venue, or <see langword="null"/>.</returns>
    Task<Venue?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Gets a venue for reading, or <see langword="null"/> if it does not exist.</summary>
    /// <param name="id">The venue id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The venue, or <see langword="null"/>.</returns>
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists a tenant's venues, newest first.</summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="includeArchived">Whether to include archived venues.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant's venues.</returns>
    Task<IReadOnlyList<Venue>> ListForTenantAsync(
        Guid tenantId,
        bool includeArchived,
        CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
