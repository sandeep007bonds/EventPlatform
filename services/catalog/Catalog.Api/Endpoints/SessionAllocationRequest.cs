namespace Catalog.Api.Endpoints;

/// <summary>One block-to-ticket-type binding.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">The ticket type that block should be sold under.</param>
public sealed record SessionAllocationRequest(string Code, Guid TicketTypeId);
