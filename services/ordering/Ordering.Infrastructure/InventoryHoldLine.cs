namespace Ordering.Infrastructure;

/// <summary>A held line as returned by the Inventory hold endpoint.</summary>
/// <param name="InventoryItemId">The inventory-item id, if this line is a reserved seat.</param>
/// <param name="SeatId">The Catalog seat id, if this line is a reserved seat.</param>
/// <param name="GeneralAdmissionAllocationId">The allocation id, if this line is general admission.</param>
/// <param name="Quantity">Number of admissions this line represents (1 for a reserved seat).</param>
/// <param name="TicketTypeId">The ticket type this line is sold as.</param>
/// <param name="UnitPriceMinor">Price per unit in minor currency units.</param>
/// <param name="PriceMinor">Total price of this line in minor currency units.</param>
internal sealed record InventoryHoldLine(
    Guid? InventoryItemId,
    Guid? SeatId,
    Guid? GeneralAdmissionAllocationId,
    int Quantity,
    Guid TicketTypeId,
    long UnitPriceMinor,
    long PriceMinor);
