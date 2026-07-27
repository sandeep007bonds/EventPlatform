namespace Inventory.Api.Endpoints;

/// <summary>
/// Request body for unblocking seats. The tenant is taken from the caller's token, never from this
/// body (ADR-0011).
/// </summary>
/// <param name="SeatIds">The seat ids to unblock.</param>
public sealed record UnblockSeatsRequest(IReadOnlyList<Guid> SeatIds);
