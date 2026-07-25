namespace Inventory.Api.Endpoints;

/// <summary>
/// Request body for placing a seat hold. The tenant and user are taken from the caller's token,
/// never from this body (ADR-0011).
/// </summary>
/// <param name="EventId">The event the seats belong to.</param>
/// <param name="SeatIds">The seat ids to hold.</param>
public sealed record PlaceHoldRequest(Guid EventId, IReadOnlyList<Guid> SeatIds);
