namespace Inventory.Infrastructure;

/// <summary>A general-admission section in the Catalog seat-map response.</summary>
/// <param name="Id">Section id.</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceAmount">Price per admission in the event's currency.</param>
/// <param name="Capacity">Total number of admissions sellable in this section.</param>
internal sealed record CatalogGeneralAdmissionSection(Guid Id, string PriceTier, decimal PriceAmount, int Capacity);
