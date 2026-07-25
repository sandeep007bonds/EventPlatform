namespace Inventory.Api.Endpoints;

/// <summary>Request body for converting a hold to a sale (called by the checkout saga).</summary>
/// <param name="OrderId">The order the hold is being converted for.</param>
public sealed record ConvertHoldRequest(Guid OrderId);
