namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="Venue"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface IVenueRepository
{
    /// <summary>Registers a new venue to be persisted.</summary>
    /// <param name="venue">The venue to add.</param>
    void Add(Venue venue);

    /// <summary>Gets a venue by id, or <see langword="null"/> if it does not exist.</summary>
    /// <param name="id">The venue id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The venue, or <see langword="null"/>.</returns>
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists a tenant's own venues — there is no public venue directory in this pass, only an
    /// organizer's own reusable-venue picker.
    /// </summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The page of venues and the total count of the tenant's venues.</returns>
    Task<(IReadOnlyList<Venue> Items, int TotalCount)> ListForTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
