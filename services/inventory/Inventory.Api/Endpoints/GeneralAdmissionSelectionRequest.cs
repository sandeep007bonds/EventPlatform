namespace Inventory.Api.Endpoints;

/// <summary>A requested quantity of one general-admission allocation, as part of a hold request.</summary>
/// <param name="AllocationId">The general-admission allocation id.</param>
/// <param name="Quantity">Number of admissions requested (positive).</param>
public sealed record GeneralAdmissionSelectionRequest(Guid AllocationId, int Quantity);
