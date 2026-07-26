namespace Ordering.Workflow;

/// <summary>Output of the create-order activity.</summary>
/// <param name="OrderId">The created order id.</param>
/// <param name="TotalMinor">Order total in minor units.</param>
/// <param name="Currency">Order currency (ISO 4217).</param>
public sealed record CreateOrderOutput(Guid OrderId, long TotalMinor, string Currency);
