namespace Inventory.Application.Abstractions;

/// <summary>Result of the Redis atomic hold attempt (the fast gate).</summary>
/// <param name="Success">Whether all seats were available and are now held.</param>
/// <param name="ConflictSeatId">The first unavailable seat, when <paramref name="Success"/> is false.</param>
public sealed record HoldStoreResult(bool Success, Guid? ConflictSeatId);
