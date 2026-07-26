namespace Ordering.Workflow;

/// <summary>Output of the checkout workflow.</summary>
/// <param name="Outcome">The <see cref="CheckoutOutcome"/> name.</param>
/// <param name="OrderId">The order id when one was created; otherwise <see langword="null"/>.</param>
public sealed record CheckoutWorkflowResult(string Outcome, Guid? OrderId);
