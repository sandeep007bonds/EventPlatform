namespace Catalog.Application.Features.SetSessionAllocations;

/// <summary>How one block is arranged for a performance, as supplied by a caller.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">
/// The ticket type that block should be sold under; <see langword="null"/> when it is excluded.
/// </param>
/// <param name="IsExcluded">Whether the block is not on sale for this performance.</param>
/// <param name="DisplayName">
/// What buyers should see the block called, or <see langword="null"/> for the venue's own name.
/// </param>
/// <param name="CapacityOverride">
/// How many to sell from an admission area, or <see langword="null"/> to sell all of it.
/// </param>
public sealed record SessionAllocationInput(
    string Code,
    Guid? TicketTypeId,
    bool IsExcluded,
    string? DisplayName,
    int? CapacityOverride);
