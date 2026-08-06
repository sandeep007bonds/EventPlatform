namespace Inventory.Api.Endpoints;

/// <summary>Request body for releasing a converted hold's sold seats/quantities (called by Ordering's cancellation saga).</summary>
/// <param name="OrderId">The order the hold was converted for.</param>
public sealed record CancelSoldRequest(Guid OrderId);
