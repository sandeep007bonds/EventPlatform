namespace Ticketing.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the local, warm-once-per-event scan cache
/// (<see cref="SessionScanContext"/>, <see cref="SeatEntryGate"/>, <see cref="GaAllocationGate"/>) —
/// what makes <c>ScanTicketAsync</c> a purely local read, with no live cross-service call.
/// Implemented in the Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface ISessionScanContextRepository
{
    /// <summary>Returns whether the event has already been provisioned (dedupe for at-least-once delivery of <c>EventPublished</c>).</summary>
    /// <param name="eventSessionId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the event already has a scan context.</returns>
    Task<bool> ExistsForSessionAsync(Guid eventSessionId, CancellationToken cancellationToken);

    /// <summary>Registers the scan context row for an event to be persisted.</summary>
    /// <param name="context">The scan context to add.</param>
    void AddContext(SessionScanContext context);

    /// <summary>Registers new seat-to-gate assignments to be persisted.</summary>
    /// <param name="assignments">The assignments to add.</param>
    void AddSeatGates(IEnumerable<SeatEntryGate> assignments);

    /// <summary>Registers new general-admission-allocation-to-gate assignments to be persisted.</summary>
    /// <param name="assignments">The assignments to add.</param>
    void AddGaAllocationGates(IEnumerable<GaAllocationGate> assignments);

    /// <summary>Gets an event's scan context, or <see langword="null"/> if it hasn't been provisioned yet.</summary>
    /// <param name="eventSessionId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The scan context, or <see langword="null"/>.</returns>
    Task<SessionScanContext?> GetContextAsync(Guid eventSessionId, CancellationToken cancellationToken);

    /// <summary>Gets the entry gate a reserved seat's section is restricted to, if any.</summary>
    /// <param name="seatId">The Catalog seat id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The restricted entry gate id, or <see langword="null"/> if unrestricted or unknown.</returns>
    Task<Guid?> GetGateForSeatAsync(Guid seatId, CancellationToken cancellationToken);

    /// <summary>Gets the entry gate a general-admission allocation's section is restricted to, if any.</summary>
    /// <param name="allocationId">Inventory's own allocation id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The restricted entry gate id, or <see langword="null"/> if unrestricted or unknown.</returns>
    Task<Guid?> GetGateForGaAllocationAsync(Guid allocationId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
