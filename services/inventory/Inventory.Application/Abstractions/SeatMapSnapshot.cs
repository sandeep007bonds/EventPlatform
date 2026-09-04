namespace Inventory.Application.Abstractions;

/// <summary>One Venue seat-map version, as far as Inventory reads it.</summary>
/// <param name="Seats">The reserved seats to provision individual inventory for.</param>
/// <param name="AdmissionAreas">The admission areas to provision capacity pools for.</param>
public sealed record SeatMapSnapshot(
    IReadOnlyList<SeatSnapshot> Seats,
    IReadOnlyList<AdmissionAreaSnapshot> AdmissionAreas);
