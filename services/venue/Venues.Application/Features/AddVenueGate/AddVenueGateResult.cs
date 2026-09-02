namespace Venues.Application.Features.AddVenueGate;

/// <summary>The result of adding a gate.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="GateId">The new gate's id, when one was created.</param>
public sealed record AddVenueGateResult(AddVenueGateOutcome Outcome, Guid? GateId);
