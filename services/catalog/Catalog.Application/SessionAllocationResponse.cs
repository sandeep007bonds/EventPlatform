namespace Catalog.Application;

/// <summary>How one block of the venue is arranged for one performance.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">
/// The ticket type that block is sold under; <see langword="null"/> when it is excluded.
/// </param>
/// <param name="IsExcluded">Whether the block is not on sale for this performance.</param>
/// <param name="DisplayName">
/// What buyers see it called, or <see langword="null"/> to use the venue's own name for it.
/// </param>
/// <param name="CapacityOverride">
/// How many to sell from an admission area, or <see langword="null"/> to sell all of it.
/// </param>
public sealed record SessionAllocationResponse(
    string Code,
    Guid? TicketTypeId,
    bool IsExcluded,
    string? DisplayName,
    int? CapacityOverride);
