namespace Inventory.Application.Abstractions;

/// <summary>
/// A general-admission section as read from the Catalog seat map, used to provision a
/// <see cref="Inventory.Domain.GeneralAdmissionAllocation"/> capacity pool.
/// </summary>
/// <param name="SectionId">The Catalog general-admission section id (stable across services).</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceAmount">Price per admission in the event's currency.</param>
/// <param name="Capacity">Total number of admissions sellable in this section.</param>
public sealed record GeneralAdmissionSectionSnapshot(Guid SectionId, string PriceTier, decimal PriceAmount, int Capacity);
