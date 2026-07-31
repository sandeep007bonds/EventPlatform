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

    /// <summary>Marks the seats held by a hold as sold (permanent).</summary>
    /// <param name="eventId">The event the seats belong to.</param>
    /// <param name="holdId">The hold being converted.</param>
    /// <param name="seatIds">The seats to mark sold.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the seats are marked sold.</returns>
    Task MarkSoldAsync(Guid eventId, Guid holdId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken);

    /// <summary>Marks the given seats blocked (organizer-initiated; not tied to a hold).</summary>
    /// <param name="eventId">The event the seats belong to.</param>
    /// <param name="seatIds">The seats to block.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the seats are marked blocked.</returns>
    Task BlockAsync(Guid eventId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken);

    /// <summary>Clears the blocked marker for the given seats, returning them to available.</summary>
    /// <param name="eventId">The event the seats belong to.</param>
    /// <param name="seatIds">The seats to unblock.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the seats are unblocked.</returns>
    Task UnblockAsync(Guid eventId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether the store has been initialized since its last (re)start. A store that returns
    /// <see langword="false"/> was just started or flushed and must be rebuilt from Postgres.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the store is warm; <see langword="false"/> if it needs a rebuild.</returns>
    Task<bool> IsInitializedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the given authoritative seat restrictions (held/sold) back into the store during a
    /// rebuild. This only ever adds restrictions — it never frees a seat — so it cannot itself cause
    /// oversell even if it races a concurrent hold.
    /// </summary>
    /// <param name="states">The authoritative non-available seat states from Postgres.</param>
    /// <param name="now">The current time (UTC), used to compute held-seat TTLs.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the states have been applied.</returns>
    Task RestoreAsync(IReadOnlyList<SeatReconciliationState> states, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Marks the store initialized so subsequent rebuild checks are skipped until the next flush.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the store is marked initialized.</returns>
    Task MarkInitializedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Initializes a general-admission allocation's remaining-capacity counter at provisioning
    /// time. A counter that was never initialized (or was lost to a Redis flush) reads as zero
    /// remaining — fail-closed, unlike the sparse seat model's fail-open default — since Postgres
    /// stays authoritative either way and a real allocation, once initialized, is not expected to
    /// disappear from Redis outside a flush.
    /// </summary>
    /// <param name="eventId">The event the allocation belongs to.</param>
    /// <param name="allocationId">The allocation id.</param>
    /// <param name="totalCapacity">The allocation's total capacity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the counter is initialized.</returns>
    Task InitializeGeneralAdmissionCapacityAsync(
        Guid eventId,
        Guid allocationId,
        int totalCapacity,
        CancellationToken cancellationToken);

    /// <summary>Atomically holds a quantity from each allocation, if every one has enough remaining capacity.</summary>
    /// <param name="eventId">The event the allocations belong to.</param>
    /// <param name="holdId">The hold id being placed.</param>
    /// <param name="selections">The (allocation id, quantity) pairs to hold.</param>
    /// <param name="ttl">How long the hold marker lives.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The hold result (success, or the conflicting allocation).</returns>
    Task<GeneralAdmissionHoldStoreResult> TryHoldGeneralAdmissionAsync(
        Guid eventId,
        Guid holdId,
        IReadOnlyList<(Guid AllocationId, int Quantity)> selections,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    /// <summary>Releases a hold's general-admission quantities, returning them to each allocation's remaining capacity.</summary>
    /// <param name="eventId">The event the allocations belong to.</param>
    /// <param name="holdId">The hold being released.</param>
    /// <param name="selections">The (allocation id, quantity) pairs to release.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the quantities are released.</returns>
    Task ReleaseGeneralAdmissionAsync(
        Guid eventId,
        Guid holdId,
        IReadOnlyList<(Guid AllocationId, int Quantity)> selections,
        CancellationToken cancellationToken);

    /// <summary>Marks a hold's general-admission quantities as sold (permanent — not returned to remaining capacity).</summary>
    /// <param name="eventId">The event the allocations belong to.</param>
    /// <param name="holdId">The hold being converted.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the hold's general-admission bookkeeping is cleared.</returns>
    Task MarkGeneralAdmissionSoldAsync(Guid eventId, Guid holdId, CancellationToken cancellationToken);
}
