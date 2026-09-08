namespace Catalog.Domain;

/// <summary>
/// One block's arrangement for a performance, as a caller of
/// <see cref="EventSession.SetAllocations"/> supplies it.
/// </summary>
/// <remarks>
/// A named record rather than the tuple this used to be. It carries five values now, two of them
/// nullable and two of them adjacent strings, which is exactly the shape a caller transposes
/// without the compiler noticing.
/// </remarks>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">
/// The ticket type the block sells as; <see langword="null"/> only when it is excluded.
/// </param>
/// <param name="IsExcluded">Whether the block is deliberately not on sale this performance.</param>
/// <param name="DisplayName">What buyers see it called, or <see langword="null"/> for the venue's own name.</param>
/// <param name="CapacityOverride">How many to sell from an admission area, or <see langword="null"/> for all of it.</param>
public sealed record SessionAllocationSpec(
    string Code,
    Guid? TicketTypeId,
    bool IsExcluded,
    string? DisplayName,
    int? CapacityOverride);
