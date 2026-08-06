namespace Queue.Application.Abstractions;

/// <summary>Persistence port for <see cref="QueueSettings"/>.</summary>
public interface IQueueSettingsRepository
{
    /// <summary>Stages a new settings row for insertion.</summary>
    /// <param name="settings">The settings to add.</param>
    void Add(QueueSettings settings);

    /// <summary>Fetches an event's settings, if provisioned.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The settings, or <see langword="null"/> if not yet provisioned.</returns>
    Task<QueueSettings?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Fetches an event's settings, scoped to a tenant — for the organizer-facing endpoints.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="tenantId">The caller's tenant id; must match the settings' owning tenant.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The settings, or <see langword="null"/> if not provisioned or owned by a different tenant.</returns>
    Task<QueueSettings?> GetForTenantAsync(Guid eventId, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Whether an event already has provisioned settings — the provisioning idempotency guard.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if already provisioned.</returns>
    Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Lists every event with queueing enabled — read by the admission controller each tick.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The enabled settings rows.</returns>
    Task<IReadOnlyList<QueueSettings>> ListEnabledAsync(CancellationToken cancellationToken);

    /// <summary>Persists staged changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
