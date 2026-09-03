namespace Catalog.Application;

/// <summary>What one block of the venue is sold as, for one performance.</summary>
/// <param name="Code">The Venue seat-map section or admission-area code.</param>
/// <param name="TicketTypeId">The ticket type that block is sold under.</param>
public sealed record SessionAllocationResponse(string Code, Guid TicketTypeId);
