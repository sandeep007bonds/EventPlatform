namespace Catalog.Application.Features.ListEntryGates;

/// <summary>Read model for a single entry gate.</summary>
/// <param name="Id">Entry-gate id.</param>
/// <param name="EventId">The event the gate belongs to.</param>
/// <param name="Name">Gate name.</param>
public sealed record EntryGateResponse(Guid Id, Guid EventId, string Name);
