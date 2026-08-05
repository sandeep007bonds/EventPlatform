namespace Ticketing.Infrastructure;

/// <summary>Subset of Inventory's <c>GET /v1/events/{id}/inventory/general-admission</c> response entry needed to resolve a gate.</summary>
/// <param name="AllocationId">Inventory's own allocation id.</param>
/// <param name="CatalogSectionId">The Catalog general-admission section id this allocation maps to.</param>
internal sealed record InventoryGaAllocationDto(Guid AllocationId, Guid CatalogSectionId);
