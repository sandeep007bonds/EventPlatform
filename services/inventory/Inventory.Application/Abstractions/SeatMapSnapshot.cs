namespace Inventory.Application.Abstractions;

/// <summary>The full Catalog seat map needed to provision inventory — both allocation kinds.</summary>
/// <param name="Seats">The reserved seats to provision reserved inventory for.</param>
/// <param name="GeneralAdmissionSections">The general-admission sections to provision allocations for.</param>
public sealed record SeatMapSnapshot(
    IReadOnlyList<SeatSnapshot> Seats,
    IReadOnlyList<GeneralAdmissionSectionSnapshot> GeneralAdmissionSections);
