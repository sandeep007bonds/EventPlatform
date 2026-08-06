namespace Inventory.Application.Provisioning;

/// <summary>
/// Applies a manual sales pause/resume from Catalog's <c>EventSalesPaused</c>/<c>EventSalesResumed</c>
/// to the cached <see cref="EventInventorySettings"/> row, so <c>HoldService.PlaceHoldAsync</c> can
/// reject new holds without a live call back to Catalog.
/// </summary>
/// <param name="inventory">The inventory repository.</param>
public sealed class EventSalesToggleService(IInventoryRepository inventory)
{
    /// <summary>
    /// Sets the event's manual sales-paused flag. A no-op if the event hasn't been provisioned yet
    /// (redelivery ordering edge case — provisioning always follows Catalog's publish, and pausing
    /// an unpublished event is not a reachable state in Catalog).
    /// </summary>
    /// <param name="eventId">The event.</param>
    /// <param name="salesPaused">The new paused state.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the flag has been applied (or immediately, if the event isn't provisioned).</returns>
    public async Task SetSalesPausedAsync(Guid eventId, bool salesPaused, CancellationToken cancellationToken)
    {
        var settings = await inventory.GetEventInventorySettingsAsync(eventId, cancellationToken);
        if (settings is null)
        {
            return;
        }

        settings.SetSalesPaused(salesPaused);
        await inventory.SaveChangesAsync(cancellationToken);
    }
}
