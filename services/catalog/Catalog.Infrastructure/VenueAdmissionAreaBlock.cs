namespace Catalog.Infrastructure;

/// <summary>An unreserved capacity area of a Venue seat map, as far as Catalog reads it.</summary>
/// <param name="Code">The stable code allocations bind to.</param>
/// <param name="Capacity">How many people it holds.</param>
internal sealed record VenueAdmissionAreaBlock(string Code, int Capacity);
