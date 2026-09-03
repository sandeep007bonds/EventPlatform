namespace Catalog.Infrastructure;

/// <summary>A section or admission area of a Venue seat map, as far as Catalog reads it.</summary>
/// <param name="Code">The stable code allocations bind to.</param>
internal sealed record VenueBlock(string Code);
