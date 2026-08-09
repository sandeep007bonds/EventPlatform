namespace Ordering.Application.Abstractions;

/// <summary>
/// Talks to the Inventory service (via Dapr service invocation) for the checkout saga:
/// read/validate a hold, convert it to a sale, or release it (compensation).
/// </summary>
public interface IHoldClient
{
    /// <summary>Reads a hold for validation and pricing.</summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The hold, or <see langword="null"/> if it does not exist.</returns>
    Task<HoldSnapshot?> GetHoldAsync(Guid holdId, CancellationToken cancellationToken);

    /// <summary>Converts a hold to a sale for an order (idempotent).</summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="orderId">The order the hold is converted for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the hold was converted.</returns>
    Task<bool> ConvertAsync(Guid holdId, Guid orderId, CancellationToken cancellationToken);

    /// <summary>Releases a hold (compensation).</summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the hold was released.</returns>
    Task<bool> ReleaseAsync(Guid holdId, CancellationToken cancellationToken);

    /// <summary>
    /// Extends a hold's expiry once payment authentication begins, so a slow 3-D Secure challenge or
    /// UPI app-switch doesn't expire the buyer's seats mid-checkout.
    /// </summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The hold's new expiry, or <see langword="null"/> if it no longer exists.</returns>
    Task<DateTimeOffset?> ExtendAsync(Guid holdId, CancellationToken cancellationToken);

    /// <summary>
    /// Releases a converted hold's sold seats/quantities back to available — a buyer-initiated
    /// cancellation/refund, the reverse of <see cref="ConvertAsync"/>.
    /// </summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="orderId">The order the hold was converted for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the hold's sold inventory was released.</returns>
    Task<bool> CancelSoldAsync(Guid holdId, Guid orderId, CancellationToken cancellationToken);
}
