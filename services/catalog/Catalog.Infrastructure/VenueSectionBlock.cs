namespace Catalog.Infrastructure;

/// <summary>A reserved-seating section of a Venue seat map, as far as Catalog reads it.</summary>
/// <param name="Code">The stable code allocations bind to.</param>
/// <param name="SellableSeatCount">How many of its seats can actually be sold.</param>
internal sealed record VenueSectionBlock(string Code, int SellableSeatCount);
