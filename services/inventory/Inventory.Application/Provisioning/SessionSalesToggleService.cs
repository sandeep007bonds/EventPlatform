namespace Inventory.Application.Provisioning;

/// <summary>
/// Applies a manual sales pause/resume from Catalog's <c>EventSalesPaused</c>/<c>EventSalesResumed</c>
/// to the cached <see cref="SessionInventorySettings"/> row, so <c>HoldService.PlaceHoldAsync</c>
/// can reject new holds without a live call back to Catalog.
/// </summary>
/// <remarks>
/// Per performance. Pausing a whole event arrives here as one message per performance, because
/// Inventory has no way to expand "the event" into the nights it consists of.
/// </remarks>
/// <param name="inventory">The inventory repository.</param>
public sealed class SessionSalesToggleService(IInventoryRepository inventory)
{
    /// <summary>
    /// Sets the performance's manual sales-paused flag. A no-op if it has not been provisioned yet
    /// (a redelivery-ordering edge case — provisioning always follows Catalog's publish, and pausing
    /// an unpublished performance is not a reachable state in Catalog).
    /// </summary>
    /// <param name="eventSessionId">The performance.</param>
    /// <param name="salesPaused">The new paused state.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the flag has been applied, or immediately if it is not provisioned.</returns>
    public async Task SetSalesPausedAsync(Guid eventSessionId, bool salesPaused, CancellationToken cancellationToken)
    {
        var settings = await inventory.GetSessionInventorySettingsAsync(eventSessionId, cancellationToken);
        if (settings is null)
        {
            return;
        }

        settings.SetSalesPaused(salesPaused);
        await inventory.SaveChangesAsync(cancellationToken);
    }
}
