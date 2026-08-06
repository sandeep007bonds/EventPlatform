namespace Ordering.Workflow;

/// <summary>Input to the order-cancellation saga.</summary>
/// <param name="OrderId">The order to cancel.</param>
/// <param name="UserId">The buyer requesting the cancellation.</param>
public sealed record CancelOrderWorkflowInput(Guid OrderId, Guid UserId);
