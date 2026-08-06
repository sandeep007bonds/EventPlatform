namespace Ordering.Workflow;

/// <summary>Input to the release-sold-inventory activity.</summary>
/// <param name="HoldId">The hold whose sold inventory should be released.</param>
/// <param name="OrderId">The order the hold was converted for.</param>
public sealed record CancelSoldInput(Guid HoldId, Guid OrderId);
