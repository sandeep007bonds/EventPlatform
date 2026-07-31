namespace Ordering.Api.Endpoints;

/// <summary>
/// Read model for a single order line — either a reserved seat (<see cref="SeatId"/> set,
/// <see cref="Quantity"/> always 1) or a general-admission quantity
/// (<see cref="GeneralAdmissionAllocationId"/> set), never both.
/// </summary>
/// <param name="SeatId">The Catalog seat id, if this line is a reserved seat.</param>
/// <param name="GeneralAdmissionAllocationId">The allocation id, if this line is general admission.</param>
/// <param name="Quantity">Number of admissions this line represents (1 for a reserved seat).</param>
/// <param name="UnitPriceMinor">Price per unit in minor currency units.</param>
/// <param name="PriceMinor">Total price of this line in minor currency units.</param>
public sealed record OrderLineResponse(
    Guid? SeatId,
    Guid? GeneralAdmissionAllocationId,
    int Quantity,
    long UnitPriceMinor,
    long PriceMinor);
