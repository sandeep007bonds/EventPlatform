namespace Inventory.Infrastructure;

/// <summary>An unreserved capacity area, as far as Inventory reads it.</summary>
/// <param name="Id">The Venue admission-area id.</param>
/// <param name="Code">The area's stable code — what the allocation map binds to.</param>
/// <param name="Capacity">How many people the area physically holds.</param>
internal sealed record VenueAdmissionArea(Guid Id, string Code, int Capacity);
