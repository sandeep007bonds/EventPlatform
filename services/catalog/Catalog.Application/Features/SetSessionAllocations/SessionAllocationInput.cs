namespace Catalog.Application.Features.SetSessionAllocations;

/// <summary>One block-to-ticket-type binding as supplied by a caller.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">The ticket type that block should be sold under.</param>
public sealed record SessionAllocationInput(string Code, Guid TicketTypeId);
