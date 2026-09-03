namespace Catalog.Application.Features.UpdateSellingRules;

/// <summary>The result of updating an event's selling rules.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Message">Why it was refused, when it was.</param>
public sealed record UpdateSellingRulesResult(UpdateSellingRulesOutcome Outcome, string? Message);
