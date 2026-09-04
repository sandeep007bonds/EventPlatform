namespace Inventory.Application.Abstractions;

/// <summary>
/// An admission area as read from a Venue seat-map version, used to provision a
/// <see cref="Inventory.Domain.GeneralAdmissionAllocation"/> capacity pool.
/// </summary>
/// <param name="AdmissionAreaId">The Venue admission-area id (stable across services).</param>
/// <param name="Code">The area's code — what the allocation map binds to.</param>
/// <param name="Capacity">How many people the area physically holds.</param>
public sealed record AdmissionAreaSnapshot(Guid AdmissionAreaId, string Code, int Capacity);
