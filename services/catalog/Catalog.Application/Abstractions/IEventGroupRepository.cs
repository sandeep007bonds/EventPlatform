namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="EventGroup"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface IEventGroupRepository
{
    /// <summary>Registers a new event group to be persisted.</summary>
    /// <param name="eventGroup">The event group to add.</param>
    void Add(EventGroup eventGroup);

    /// <summary>Gets an event group by id, or <see langword="null"/> if it does not exist.</summary>
    /// <param name="id">The event group id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event group, or <see langword="null"/>.</returns>
    Task<EventGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Lists a tenant's own event groups — there is no public directory in this pass, only an
    /// organizer's own "pick or create a tour" picker.
    /// </summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The page of event groups and the total count of the tenant's event groups.</returns>
    Task<(IReadOnlyList<EventGroup> Items, int TotalCount)> ListForTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets multiple event groups by id in one round trip — used to resolve contact/social
    /// fallbacks for a page of events without one query per event.
    /// </summary>
    /// <param name="ids">The event group ids to fetch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching event groups (fewer than requested if some ids don't exist).</returns>
    Task<IReadOnlyList<EventGroup>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
