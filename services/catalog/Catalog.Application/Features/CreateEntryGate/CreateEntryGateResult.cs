namespace Catalog.Application.Features.CreateEntryGate;

/// <summary>Outcome of a <see cref="CreateEntryGateCommand"/>, with the new gate's id when created.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="EntryGateId">The new gate's id when created; otherwise <see langword="null"/>.</param>
public sealed record CreateEntryGateResult(CreateEntryGateOutcome Outcome, Guid? EntryGateId);
