namespace Ordering.Workflow;

/// <summary>Input to the fail-order activity (compensation).</summary>
/// <param name="OrderId">The order to fail.</param>
/// <param name="Reason">Why the order failed.</param>
public sealed record FailInput(Guid OrderId, string Reason);
