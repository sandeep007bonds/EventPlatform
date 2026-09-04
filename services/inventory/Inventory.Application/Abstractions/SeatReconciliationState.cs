namespace Inventory.Application.Abstractions;

/// <summary>
/// The authoritative state of a single non-available seat, read from Postgres, that the reconciler
/// writes back into the Redis fast gate after a Redis restart or flush.
/// </summary>
/// <param name="EventSessionId">The event the seat belongs to.</param>
/// <param name="SeatId">The Catalog seat id.</param>
/// <param name="Condition">Whether the seat is held or sold.</param>
/// <param name="HoldId">The active hold id when <see cref="Condition"/> is
/// <see cref="SeatCondition.Held"/>; otherwise <see langword="null"/>.</param>
/// <param name="HoldExpiresAt">The hold's expiry (UTC) when held, used to set the Redis TTL;
/// otherwise <see langword="null"/>.</param>
public sealed record SeatReconciliationState(
    Guid EventSessionId,
    Guid SeatId,
    SeatCondition Condition,
    Guid? HoldId,
    DateTimeOffset? HoldExpiresAt);
