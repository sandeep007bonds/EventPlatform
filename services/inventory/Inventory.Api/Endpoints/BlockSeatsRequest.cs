namespace Inventory.Api.Endpoints;

/// <summary>
/// Request body for blocking seats. The tenant is taken from the caller's token, never from this
/// body (ADR-0011).
/// </summary>
/// <param name="SeatIds">The seat ids to block.</param>
/// <param name="Reason">Optional organizer-supplied reason (e.g. <c>"restricted view"</c>).</param>
public sealed record BlockSeatsRequest(IReadOnlyList<Guid> SeatIds, string? Reason);
