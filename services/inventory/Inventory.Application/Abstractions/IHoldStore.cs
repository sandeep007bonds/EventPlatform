namespace Inventory.Application.Abstractions;

/// <summary>
/// The Redis fast gate for no-oversell: an atomic (Lua) check-and-set over seat status keys.
/// Postgres remains the final authority; this store is the fast rejecter under contention.
/// </summary>
public interface IHoldStore
{
    /// <summary>Atomically holds all seats if every one is available.</summary>
    /// <param name="eventId">The event the seats belong to.</param>
    /// <param name="holdId">The hold id being placed.</param>
    /// <param name="seatIds">The seats to hold.</param>
    /// <param name="ttl">How long the hold marker lives.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The hold result (success, or the conflicting seat).</returns>
    Task<HoldStoreResult> TryHoldAsync(
        Guid eventId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    /// <summary>Releases the seats held by a hold, returning them to available.</summary>
    /// <param name="eventId">The event the seats belong to.</param>
    /// <param name="holdId">The hold being released.</param>
    /// <param name="seatIds">The seats to release.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the seats are released.</returns>
    Task ReleaseAsync(Guid eventId, Guid holdId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken);
}
