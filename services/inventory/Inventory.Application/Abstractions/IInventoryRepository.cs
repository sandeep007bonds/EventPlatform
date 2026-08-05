namespace Inventory.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="InventoryItem"/>. Implemented in the Infrastructure
/// layer so the Application layer stays free of EF Core.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>Registers new inventory items to be persisted.</summary>
    /// <param name="items">The items to add.</param>
    void AddRange(IEnumerable<InventoryItem> items);

    /// <summary>
    /// Returns whether the event has already been provisioned (dedupe for at-least-once delivery of
    /// <c>EventPublished</c>). Backed by <see cref="EventInventorySettings"/> rather than seat count,
    /// since an event made entirely of general-admission sections may provision zero seats.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the event already has inventory.</returns>
    Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Counts the inventory items for an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of inventory items.</returns>
    Task<int> CountForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Lists all inventory items for an event, with their current status.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event's inventory items.</returns>
    Task<IReadOnlyList<InventoryItem>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Loads the tracked inventory items for the given seats of an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="seatIds">The seat ids.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching inventory items (tracked).</returns>
    Task<IReadOnlyList<InventoryItem>> GetItemsBySeatsAsync(
        Guid eventId,
        IReadOnlyCollection<Guid> seatIds,
        CancellationToken cancellationToken);

    /// <summary>Loads the tracked inventory items with the given ids.</summary>
    /// <param name="itemIds">The inventory-item ids.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching inventory items (tracked).</returns>
    Task<IReadOnlyList<InventoryItem>> GetItemsByIdsAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken);

    /// <summary>Loads a tracked hold (with its items) by id.</summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The hold, or <see langword="null"/>.</returns>
    Task<Hold?> GetHoldAsync(Guid holdId, CancellationToken cancellationToken);

    /// <summary>Gets the ids of active holds that have passed their expiry (for the reaper).</summary>
    /// <param name="now">The current time (UTC).</param>
    /// <param name="batchSize">Maximum ids to return.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Expired active hold ids, oldest first.</returns>
    Task<IReadOnlyList<Guid>> GetExpiredActiveHoldIdsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>Gets the ids of events that currently have any held or sold inventory (for reconciliation).</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The distinct event ids with non-available inventory.</returns>
    Task<IReadOnlyList<Guid>> GetEventIdsWithActiveInventoryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the authoritative non-available seat states (held/sold) for an event, so the Redis fast
    /// gate can be rebuilt after a restart or flush.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The held and sold seat states for the event.</returns>
    Task<IReadOnlyList<SeatReconciliationState>> GetReconciliationStateAsync(
        Guid eventId,
        CancellationToken cancellationToken);

    /// <summary>Registers a new hold to be persisted.</summary>
    /// <param name="hold">The hold to add.</param>
    void AddHold(Hold hold);

    /// <summary>Registers ledger entries to be persisted.</summary>
    /// <param name="entries">The entries to add.</param>
    void AddLedgerEntries(IEnumerable<LedgerEntry> entries);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists all pending changes, returning <see langword="false"/> instead of throwing when an
    /// optimistic-concurrency conflict occurs (a lost race — the final no-oversell rejecter).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if saved; <see langword="false"/> on a concurrency conflict.</returns>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Registers new general-admission allocations to be persisted.</summary>
    /// <param name="allocations">The allocations to add.</param>
    void AddGeneralAdmissionAllocations(IEnumerable<GeneralAdmissionAllocation> allocations);

    /// <summary>Counts the general-admission allocations for an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of general-admission allocations.</returns>
    Task<int> CountGeneralAdmissionAllocationsForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Loads the tracked general-admission allocations with the given ids.</summary>
    /// <param name="allocationIds">The allocation ids.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The matching allocations (tracked).</returns>
    Task<IReadOnlyList<GeneralAdmissionAllocation>> GetGeneralAdmissionAllocationsByIdsAsync(
        IReadOnlyCollection<Guid> allocationIds,
        CancellationToken cancellationToken);

    /// <summary>Returns every general-admission allocation for an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event's general-admission allocations.</returns>
    Task<IReadOnlyList<GeneralAdmissionAllocation>> ListGeneralAdmissionForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Loads the event inventory settings for an event, if provisioned.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The settings, or <see langword="null"/> if the event has no settings row yet.</returns>
    Task<EventInventorySettings?> GetEventInventorySettingsAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Registers a new event inventory settings row to be persisted.</summary>
    /// <param name="settings">The settings to add.</param>
    void AddEventInventorySettings(EventInventorySettings settings);

    /// <summary>
    /// Sums a buyer's already-committed quantity for an event — seats plus general-admission
    /// quantities — across their <see cref="HoldStatus.Active"/> and <see cref="HoldStatus.Converted"/>
    /// holds. Used to enforce <see cref="EventInventorySettings.MaxTicketsPerBuyer"/> before a new
    /// hold is placed. Released holds are excluded — that quantity has freed back up.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The buyer's total already-committed quantity for the event.</returns>
    Task<int> GetBuyerCommittedQuantityAsync(Guid eventId, Guid userId, CancellationToken cancellationToken);
}
