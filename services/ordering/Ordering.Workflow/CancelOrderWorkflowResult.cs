namespace Ordering.Workflow;

/// <summary>Output of the order-cancellation workflow.</summary>
/// <param name="Outcome">The <see cref="CancelOrderOutcome"/> name.</param>
/// <param name="OrderId">The order id, when known; otherwise <see langword="null"/>.</param>
public sealed record CancelOrderWorkflowResult(string Outcome, Guid? OrderId);
