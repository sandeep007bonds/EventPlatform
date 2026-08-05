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

    /// <summary>
    /// Lists events visible to the given caller: everyone sees non-draft events; a caller with a
    /// tenant id additionally sees that tenant's drafts. See <see cref="Event.IsVisibleTo"/>.
    /// </summary>
    /// <param name="callerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
    /// <param name="status">An optional status filter, applied within the caller's visible set.</param>
    /// <param name="eventGroupId">An optional filter to only the legs of a given tour/series.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The page of events and the total count of visible events matching the filter.</returns>
    Task<(IReadOnlyList<Event> Items, int TotalCount)> ListAsync(
        Guid? callerTenantId,
        EventStatus? status,
        Guid? eventGroupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists a single tenant's own events at any status (an organizer dashboard) — bypasses the
    /// public visibility rule entirely, since it's the tenant's own data.
    /// </summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="status">An optional status filter.</param>
    /// <param name="eventGroupId">An optional filter to only the legs of a given tour/series.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The page of events and the total count of the tenant's events matching the filter.</returns>
    Task<(IReadOnlyList<Event> Items, int TotalCount)> ListForTenantAsync(
        Guid tenantId,
        EventStatus? status,
        Guid? eventGroupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists every leg (event) belonging to a tour, at any status, regardless of tenant visibility
    /// — used only for cross-leg date-range validation (containment in the tour's range, no
    /// overlap between siblings), never exposed to a caller directly.
    /// </summary>
    /// <param name="eventGroupId">The tour/series id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tour's legs.</returns>
    Task<IReadOnlyList<Event>> ListLegsForEventGroupAsync(Guid eventGroupId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
