namespace Catalog.Application.Features.CreateEvent;

/// <summary>Outcome of a <see cref="CreateEventCommand"/>, with the new event's id when created.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="EventId">The new event's id when created; otherwise <see langword="null"/>.</param>
public sealed record CreateEventResult(CreateEventOutcome Outcome, Guid? EventId);
