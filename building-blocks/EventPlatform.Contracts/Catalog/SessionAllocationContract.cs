namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// One block of a venue, the ticket type it is sold as for a performance, and that type's price at
/// the moment the performance went on sale.
/// </summary>
/// <remarks>
/// The price is a <b>snapshot</b>, not a reference. Inventory holds it so a hold can be quoted
/// without a call to Catalog, and Ordering re-derives the charged amount from the order's own
/// snapshot — nothing downstream treats this as the live price.
/// </remarks>
/// <param name="Code">The Venue seat-map section or admission-area code this covers.</param>
/// <param name="TicketTypeId">The ticket type the block is sold under.</param>
/// <param name="PriceMinor">That type's price in minor currency units, at publish time.</param>
/// <param name="CapacityOverride">
/// How many to sell from an admission area when the performance sells fewer than it holds;
/// <see langword="null"/> to sell all of it. <b>Admission areas only</b> — a reserved section's
/// capacity is seats with identity, so capping it to a number would not say which seats, and
/// Inventory already blocks individual seats per performance for that.
/// </param>
public sealed record SessionAllocationContract(
    string Code,
    Guid TicketTypeId,
    long PriceMinor,
    int? CapacityOverride);
