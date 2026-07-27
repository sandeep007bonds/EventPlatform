namespace Inventory.Application.Blocking;

/// <summary>Outcome of a block-seats attempt, with the conflicting seat when applicable.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="ConflictSeatId">The conflicting seat id when known; otherwise <see langword="null"/>.</param>
public sealed record BlockSeatsResult(BlockSeatsOutcome Outcome, Guid? ConflictSeatId)
{
    /// <summary>All requested seats are now blocked.</summary>
    /// <returns>A blocked result.</returns>
    public static BlockSeatsResult Blocked() => new(BlockSeatsOutcome.Blocked, null);

    /// <summary>One or more requested seats do not exist for this event.</summary>
    /// <returns>A not-found result.</returns>
    public static BlockSeatsResult SeatNotFound() => new(BlockSeatsOutcome.SeatNotFound, null);

    /// <summary>A conflict (a seat was not available).</summary>
    /// <param name="seatId">The conflicting seat id, if known.</param>
    /// <returns>A conflict result.</returns>
    public static BlockSeatsResult Conflict(Guid? seatId) => new(BlockSeatsOutcome.Conflict, seatId);
}
