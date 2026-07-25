namespace Inventory.Application.Provisioning;

/// <summary>Outcome of provisioning inventory for an event.</summary>
/// <param name="Provisioned">
/// <see langword="true"/> if inventory was generated; <see langword="false"/> if the event was
/// already provisioned (idempotent no-op).
/// </param>
/// <param name="SeatCount">Number of inventory items generated (0 when already provisioned).</param>
public sealed record ProvisioningResult(bool Provisioned, int SeatCount);
