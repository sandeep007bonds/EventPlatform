namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Read model for a single general-admission section — a capacity pool, no individual seats.</summary>
/// <param name="Id">Stable section id (shared across services — Inventory references it directly).</param>
/// <param name="SectionName">Section name.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceAmount">Price in the event's currency.</param>
/// <param name="Capacity">Total number of admissions sellable in this section.</param>
public sealed record GeneralAdmissionSectionResponse(
    Guid Id,
    string SectionName,
    string PriceTier,
    decimal PriceAmount,
    int Capacity);
