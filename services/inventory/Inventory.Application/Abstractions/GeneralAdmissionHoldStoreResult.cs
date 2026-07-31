namespace Inventory.Application.Abstractions;

/// <summary>Result of the Redis atomic general-admission hold attempt (the fast gate).</summary>
/// <param name="Success">Whether every requested quantity fit within its allocation's remaining capacity and is now held.</param>
/// <param name="ConflictAllocationId">The first allocation without enough remaining capacity, when <paramref name="Success"/> is false.</param>
public sealed record GeneralAdmissionHoldStoreResult(bool Success, Guid? ConflictAllocationId);
