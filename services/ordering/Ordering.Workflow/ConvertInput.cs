namespace Ordering.Workflow;

/// <summary>Input to the convert-to-sold activity.</summary>
/// <param name="HoldId">The hold to convert.</param>
/// <param name="OrderId">The order the hold is converted for.</param>
public sealed record ConvertInput(Guid HoldId, Guid OrderId);
