namespace Ticketing.Application.Abstractions;

/// <summary>
/// Which entry gate, if any, each reserved seat or admission area of a performance's pinned Venue
/// seat-map version is restricted to. Resolved once per performance by
/// <c>SessionScanContextProvisioningService</c>, never queried live at scan time.
/// </summary>
/// <remarks>
/// A gate belongs to the venue, and a section names one. This flattens that down to the seat,
/// because a scan knows a ticket's seat and needs an answer in one local lookup.
/// </remarks>
/// <param name="EntryGateIdBySeatId">Each reserved seat's restricted entry gate, keyed by Venue seat id (only entries with a restriction).</param>
/// <param name="EntryGateIdByAdmissionAreaId">Each admission area's restricted entry gate, keyed by Venue admission-area id (only entries with a restriction).</param>
public sealed record VenueGateMap(
    IReadOnlyDictionary<Guid, Guid> EntryGateIdBySeatId,
    IReadOnlyDictionary<Guid, Guid> EntryGateIdByAdmissionAreaId);
