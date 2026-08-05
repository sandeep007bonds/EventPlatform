namespace Inventory.Api.Endpoints;

/// <summary>One general-admission allocation's status, keyed by the Catalog section id a buyer already has.</summary>
/// <param name="AllocationId">Inventory's own allocation id — what a hold request must reference.</param>
/// <param name="CatalogSectionId">The Catalog general-admission section id this allocation maps to.</param>
/// <param name="Remaining">How many admissions are still available to hold.</param>
/// <param name="TotalCapacity">The section's total sellable capacity.</param>
public sealed record GeneralAdmissionAllocationResponse(Guid AllocationId, Guid CatalogSectionId, int Remaining, int TotalCapacity);
