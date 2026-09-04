namespace Inventory.Infrastructure;

/// <summary>A reserved-seating section of a Venue seat map, as far as Inventory reads it.</summary>
/// <param name="Code">The section's stable code — what the allocation map binds to.</param>
/// <param name="Rows">The section's rows.</param>
internal sealed record VenueSection(string Code, IReadOnlyList<VenueRow> Rows);
