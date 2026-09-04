namespace Ordering.Domain;

/// <summary>
/// Input describing one line of a new order — either a held seat
/// (<see cref="InventoryItemId"/>/<see cref="SeatId"/> set, <see cref="Quantity"/> always 1) or a
/// general-admission quantity (<see cref="GeneralAdmissionAllocationId"/> set), never both.
/// </summary>
/// <param name="InventoryItemId">The inventory-item id, if this line is a reserved seat.</param>
/// <param name="SeatId">The Venue seat id, if this line is a reserved seat.</param>
/// <param name="GeneralAdmissionAllocationId">The allocation id, if this line is general admission.</param>
/// <param name="Quantity">Number of admissions this line represents (1 for a reserved seat).</param>
/// <param name="TicketTypeId">
/// The ticket type this line was sold as, carried through from the hold. Needed to decide whether a
/// type-restricted promo code applies to this line, and to report sales by type.
/// </param>
/// <param name="UnitPriceMinor">Price per unit in minor currency units.</param>
/// <param name="PriceMinor">Total price of this line in minor currency units (<see cref="UnitPriceMinor"/> × <see cref="Quantity"/>).</param>
public sealed record OrderLineSpec(
    Guid? InventoryItemId,
    Guid? SeatId,
    Guid? GeneralAdmissionAllocationId,
    int Quantity,
    Guid TicketTypeId,
    long UnitPriceMinor,
    long PriceMinor);
