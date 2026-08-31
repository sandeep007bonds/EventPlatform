namespace Catalog.Application.Features.CreateTicketType;

/// <summary>The result of creating a ticket type.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="TicketTypeId">The new type's id, when one was created.</param>
public sealed record CreateTicketTypeResult(CreateTicketTypeOutcome Outcome, Guid? TicketTypeId);
