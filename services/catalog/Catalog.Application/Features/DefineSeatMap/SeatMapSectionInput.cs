namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>
/// One section of a seat-map definition. A <see cref="Catalog.Domain.AllocationType.Reserved"/>
/// section expands to <c>Rows × SeatsPerRow</c> individual seats; a
/// <see cref="Catalog.Domain.AllocationType.GeneralAdmission"/> section is a capacity-only pool
/// with no individual seats.
/// </summary>
/// <param name="Name">Section name; must be unique within the map (across both allocation types).</param>
/// <param name="PriceTier">Price tier name for the section.</param>
/// <param name="PriceAmount">Price (non-negative) in the event's currency.</param>
/// <param name="AllocationType">Whether this section is individually seated or general admission.</param>
/// <param name="Rows">Number of rows (positive). Required for <see cref="Catalog.Domain.AllocationType.Reserved"/>; ignored otherwise.</param>
/// <param name="SeatsPerRow">Seats per row (positive). Required for <see cref="Catalog.Domain.AllocationType.Reserved"/>; ignored otherwise.</param>
/// <param name="Capacity">Total admissions sellable (positive). Required for <see cref="Catalog.Domain.AllocationType.GeneralAdmission"/>; ignored otherwise.</param>
public sealed record SeatMapSectionInput(
    string Name,
    string PriceTier,
    decimal PriceAmount,
    AllocationType AllocationType,
    int? Rows,
    int? SeatsPerRow,
    int? Capacity);
