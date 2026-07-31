namespace Inventory.Application.Holds;

/// <summary>
/// Outcome of a place-hold attempt, with the hold details or the conflicting seat/allocation.
/// </summary>
/// <param name="Outcome">What happened.</param>
/// <param name="HoldId">The hold id when held; otherwise <see langword="null"/>.</param>
/// <param name="ExpiresAt">When the hold expires, when held; otherwise <see langword="null"/>.</param>
/// <param name="ConflictSeatId">The conflicting seat id when known; otherwise <see langword="null"/>.</param>
/// <param name="ConflictAllocationId">
/// The conflicting general-admission allocation id when known; otherwise <see langword="null"/>.
/// </param>
public sealed record PlaceHoldResult(
    PlaceHoldOutcome Outcome,
    Guid? HoldId,
    DateTimeOffset? ExpiresAt,
    Guid? ConflictSeatId,
    Guid? ConflictAllocationId)
{
    /// <summary>A successful hold.</summary>
    /// <param name="holdId">The hold id.</param>
    /// <param name="expiresAt">When the hold expires.</param>
    /// <returns>A held result.</returns>
    public static PlaceHoldResult Held(Guid holdId, DateTimeOffset expiresAt) =>
        new(PlaceHoldOutcome.Held, holdId, expiresAt, null, null);

    /// <summary>A conflict (a seat and/or a general-admission allocation was not available).</summary>
    /// <param name="conflictSeatId">The conflicting seat, if known.</param>
    /// <param name="conflictAllocationId">The conflicting general-admission allocation, if known.</param>
    /// <returns>A conflict result.</returns>
    public static PlaceHoldResult Conflict(Guid? conflictSeatId, Guid? conflictAllocationId = null) =>
        new(PlaceHoldOutcome.Conflict, null, null, conflictSeatId, conflictAllocationId);

    /// <summary>A non-conflict failure (e.g. an unknown seat or allocation).</summary>
    /// <param name="outcome">The failure outcome.</param>
    /// <returns>A failed result.</returns>
    public static PlaceHoldResult Failed(PlaceHoldOutcome outcome) =>
        new(outcome, null, null, null, null);
}
