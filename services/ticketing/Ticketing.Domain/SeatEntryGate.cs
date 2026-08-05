namespace Ticketing.Domain;

/// <summary>
/// The entry gate a reserved seat's section is restricted to, resolved once from Catalog's seat
/// map (via <c>EventScanContextProvisioningService</c>) and looked up locally at scan time —
/// never a live cross-service call. A seat with no row here may be entered through any gate.
/// </summary>
public sealed class SeatEntryGate
{
    // Parameterless ctor for EF Core materialization.
    private SeatEntryGate()
    {
    }

    private SeatEntryGate(Guid seatId, Guid eventId, Guid entryGateId)
    {
        SeatId = seatId;
        EventId = eventId;
        EntryGateId = entryGateId;
    }

    /// <summary>The Catalog seat id (primary key).</summary>
    public Guid SeatId { get; private set; }

    /// <summary>The event this seat belongs to.</summary>
    public Guid EventId { get; private set; }

    /// <summary>The entry gate this seat's section is restricted to.</summary>
    public Guid EntryGateId { get; private set; }

    /// <summary>Creates a seat-to-gate assignment row.</summary>
    /// <param name="seatId">The Catalog seat id.</param>
    /// <param name="eventId">The event the seat belongs to.</param>
    /// <param name="entryGateId">The restricted entry gate.</param>
    /// <returns>A new <see cref="SeatEntryGate"/>.</returns>
    public static SeatEntryGate Create(Guid seatId, Guid eventId, Guid entryGateId) => new(seatId, eventId, entryGateId);
}
