namespace Ticketing.Application.Abstractions;

/// <summary>
/// The Catalog data a ticket scan needs: the event's check-in window, and which entry gate (if
/// any) each reserved seat or general-admission section is restricted to.
/// </summary>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if set — falls back to <see cref="StartsAt"/> when absent.</param>
/// <param name="StartsAt">Scheduled start time (UTC).</param>
/// <param name="EndsAt">Scheduled end time (UTC) — check-in is rejected after this time.</param>
/// <param name="EntryGateIdBySeatId">Each reserved seat's restricted entry gate, keyed by seat id.</param>
/// <param name="EntryGateIdByCatalogSectionId">Each general-admission section's restricted entry gate, keyed by the Catalog section id.</param>
public sealed record CatalogScanContext(
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    IReadOnlyDictionary<Guid, Guid?> EntryGateIdBySeatId,
    IReadOnlyDictionary<Guid, Guid?> EntryGateIdByCatalogSectionId);
