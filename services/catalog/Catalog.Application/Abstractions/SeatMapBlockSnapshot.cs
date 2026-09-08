namespace Catalog.Application.Abstractions;

/// <summary>One block of a Venue seat-map version, as far as Catalog reasons about it.</summary>
/// <remarks>
/// The capacity is here because the performance no longer sells all of it. Once a block can be
/// excluded or capped, the version's own total is the building's number, not the night's, and the
/// only place that difference can be computed is where both are known.
/// </remarks>
/// <param name="Code">The stable code allocations bind to.</param>
/// <param name="Capacity">
/// What the block physically holds — sellable seats for a section, capacity for an admission area.
/// </param>
/// <param name="IsAdmissionArea">
/// Whether it is unreserved capacity rather than reserved seats. Only an area can be capped to a
/// number: "sell 200 of 400" says nothing about which 400 seats, and seats have identity.
/// </param>
public sealed record SeatMapBlockSnapshot(string Code, int Capacity, bool IsAdmissionArea);
