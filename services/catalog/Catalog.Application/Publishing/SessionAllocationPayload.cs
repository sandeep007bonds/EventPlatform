namespace Catalog.Application.Publishing;

/// <summary>One block, the ticket type it sells as, and that type's price at publish time.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">The ticket type the block is sold under.</param>
/// <param name="PriceMinor">That type's price in minor currency units, read at publish time.</param>
/// <param name="CapacityOverride">
/// How many to sell from an admission area when that is fewer than it holds, or
/// <see langword="null"/> to sell all of it.
/// </param>
public sealed record SessionAllocationPayload(
    string Code,
    Guid TicketTypeId,
    long PriceMinor,
    int? CapacityOverride);
