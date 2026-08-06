namespace Queue.Application.Provisioning;

/// <summary>
/// Provisions an event's <see cref="QueueSettings"/> once, idempotently, on receipt of Catalog's
/// <c>EventPublished</c> — mirrors <c>InventoryProvisioningService</c>/
/// <c>EventScanContextProvisioningService</c>'s exact idempotent-on-redelivery shape.
/// </summary>
/// <param name="settings">The queue-settings repository.</param>
public sealed class QueueSettingsProvisioningService(IQueueSettingsRepository settings)
{
    /// <summary>Provisions settings for an event, unless already provisioned.</summary>
    /// <param name="eventId">The published event's id.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="requiresQueue"><c>EventPublished.RequiresQueue</c> — becomes <see cref="QueueSettings.Enabled"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if a new row was provisioned; <see langword="false"/> if already present (redelivery).</returns>
    public async Task<bool> ProvisionAsync(
        Guid eventId,
        Guid tenantId,
        bool requiresQueue,
        CancellationToken cancellationToken)
    {
        if (await settings.ExistsForEventAsync(eventId, cancellationToken))
        {
            return false;
        }

        settings.Add(QueueSettings.Create(eventId, tenantId, requiresQueue));
        await settings.SaveChangesAsync(cancellationToken);
        return true;
    }
}
