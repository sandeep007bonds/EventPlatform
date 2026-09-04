namespace Inventory.Api.Endpoints;

/// <summary>One general-admission allocation's status, keyed by the Catalog section id a buyer already has.</summary>
/// <param name="AllocationId">Inventory's own allocation id — what a hold request must reference.</param>
/// <param name="AdmissionAreaId">The Venue admission-area id this pool maps to.</param>
/// <param name="TicketTypeId">The Catalog ticket type this pool sells as for this performance.</param>
/// <param name="PriceMinor">The price of one admission, in minor units.</param>
/// <param name="Remaining">How many admissions are still available to hold.</param>
/// <param name="TotalCapacity">The section's total sellable capacity.</param>
/// <param name="HeldCount">Number of admissions currently held (not yet sold, not yet released).</param>
/// <param name="SoldCount">Number of admissions sold.</param>
public sealed record GeneralAdmissionAllocationResponse(
    Guid AllocationId,
    Guid AdmissionAreaId,
    Guid TicketTypeId,
    long PriceMinor,
    int Remaining,
    int TotalCapacity,
    int HeldCount,
    int SoldCount);
