namespace Ordering.Workflow;

/// <summary>Input to the promo-code evaluation activity.</summary>
/// <param name="CatalogEventId">The event being purchased.</param>
/// <param name="Code">The code the buyer typed.</param>
/// <param name="UserId">The buyer, for the per-buyer redemption cap.</param>
/// <param name="Lines">The lines being purchased, for the tier-applicability check.</param>
public sealed record EvaluatePromoCodeInput(
    Guid CatalogEventId,
    string Code,
    Guid UserId,
    IReadOnlyList<OrderLineSpec> Lines);
