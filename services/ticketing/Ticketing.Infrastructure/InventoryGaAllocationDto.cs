namespace Ticketing.Infrastructure;

/// <summary>Subset of Inventory's <c>GET /v1/sessions/{id}/inventory/general-admission</c> response entry needed to resolve a gate.</summary>
/// <param name="AllocationId">Inventory's own allocation id.</param>
/// <param name="AdmissionAreaId">The Venue admission-area id this pool maps to.</param>
internal sealed record InventoryGaAllocationDto(Guid AllocationId, Guid AdmissionAreaId);
