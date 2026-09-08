namespace Catalog.Api.Endpoints;

/// <summary>How one block of the venue is arranged for a performance.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">
/// The ticket type that block should be sold under; omit it when the block is excluded.
/// </param>
/// <param name="IsExcluded">Whether the block is not on sale for this performance.</param>
/// <param name="DisplayName">
/// What buyers should see the block called, or omitted for the venue's own name. The code is
/// unaffected — it is what inventory, tickets and scanning bind to.
/// </param>
/// <param name="CapacityOverride">
/// How many to sell from an admission area when that is fewer than it holds; omitted to sell all of
/// it. Refused on a reserved section, which is blocked seat by seat in Inventory instead.
/// </param>
public sealed record SessionAllocationRequest(
    string Code,
    Guid? TicketTypeId,
    bool IsExcluded,
    string? DisplayName,
    int? CapacityOverride);
