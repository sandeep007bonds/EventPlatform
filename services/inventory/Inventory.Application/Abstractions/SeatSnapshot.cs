namespace Inventory.Application.Abstractions;

/// <summary>A seat as read from a Venue seat-map version, used to provision inventory.</summary>
/// <remarks>
/// No price and no ticket type: a Venue seat carries neither (ADR-0038). What it belongs to
/// commercially comes from the performance's allocation map, joined on
/// <see cref="SectionCode"/> during provisioning.
/// </remarks>
/// <param name="SeatId">The Venue seat id (stable across services).</param>
/// <param name="SectionCode">The section's code — what the allocation map binds to.</param>
/// <param name="IsSellable">
/// Whether the seat can ever be sold. A non-sellable seat is still provisioned, as
/// <c>Blocked</c>, so the map renders complete rather than with a hole in it.
/// </param>
public sealed record SeatSnapshot(Guid SeatId, string SectionCode, bool IsSellable);
