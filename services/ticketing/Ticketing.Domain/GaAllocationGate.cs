namespace Ticketing.Domain;

/// <summary>
/// The entry gate a general-admission allocation's section is restricted to, resolved once (via
/// <c>SessionScanContextProvisioningService</c>, combining Catalog's seat-map gate map with
/// Inventory's allocation-to-section map) and looked up locally at scan time — never a live
/// cross-service call. An allocation with no row here may be entered through any gate.
/// </summary>
public sealed class GaAllocationGate
{
    // Parameterless ctor for EF Core materialization.
    private GaAllocationGate()
    {
    }

    private GaAllocationGate(Guid allocationId, Guid eventSessionId, Guid entryGateId)
    {
        AllocationId = allocationId;
        EventSessionId = eventSessionId;
        EntryGateId = entryGateId;
    }

    /// <summary>Inventory's own general-admission allocation id (primary key).</summary>
    public Guid AllocationId { get; private set; }

    /// <summary>The event this allocation belongs to.</summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>The entry gate this allocation's section is restricted to.</summary>
    public Guid EntryGateId { get; private set; }

    /// <summary>Creates an allocation-to-gate assignment row.</summary>
    /// <param name="allocationId">Inventory's own allocation id.</param>
    /// <param name="eventSessionId">The event the allocation belongs to.</param>
    /// <param name="entryGateId">The restricted entry gate.</param>
    /// <returns>A new <see cref="GaAllocationGate"/>.</returns>
    public static GaAllocationGate Create(Guid allocationId, Guid eventSessionId, Guid entryGateId) => new(allocationId, eventSessionId, entryGateId);
}
