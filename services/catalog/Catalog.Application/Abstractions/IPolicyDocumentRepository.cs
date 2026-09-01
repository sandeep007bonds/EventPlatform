namespace Catalog.Application.Abstractions;

/// <summary>Persistence abstraction for the <see cref="PolicyDocument"/> aggregate.</summary>
public interface IPolicyDocumentRepository
{
    /// <summary>Registers a new policy document to be persisted.</summary>
    /// <param name="document">The document to add.</param>
    void Add(PolicyDocument document);

    /// <summary>
    /// Gets one tenant's document of a given kind and scope, or <see langword="null"/> if it has
    /// never been written.
    /// </summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="eventId">The event scope, or <see langword="null"/> for the tenant default.</param>
    /// <param name="kind">Which document.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The document, or <see langword="null"/>.</returns>
    Task<PolicyDocument?> GetAsync(Guid tenantId, Guid? eventId, PolicyKind kind, CancellationToken cancellationToken);

    /// <summary>Lists a tenant's own default documents, of every kind.</summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant's defaults, which may be empty.</returns>
    Task<IReadOnlyList<PolicyDocument>> ListDefaultsAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the documents that apply to one event: its own overrides, plus the tenant defaults for
    /// every kind it does not override.
    /// </summary>
    /// <param name="tenantId">The event's owning tenant.</param>
    /// <param name="eventId">The event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// The event's overrides and the tenant defaults, unresolved — the caller applies the
    /// override-wins rule, so both are visible to an organizer editing them.
    /// </returns>
    Task<IReadOnlyList<PolicyDocument>> ListForEventAsync(Guid tenantId, Guid eventId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
