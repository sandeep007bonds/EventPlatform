namespace Ordering.Workflow;

/// <summary>
/// Re-checks a promo code inside the saga, immediately before the order is created.
/// </summary>
/// <remarks>
/// The buyer already saw a quote for this code, but that quote is advisory: a code can expire, be
/// retired, or hit its redemption cap between the preview and the confirm. Re-evaluating here is
/// what makes the charged total trustworthy, and it runs the same
/// <see cref="PromoCodeEvaluator"/> the quote endpoint does, so the two cannot disagree about the
/// arithmetic — only about a fact that genuinely changed in between.
/// </remarks>
/// <param name="evaluator">The shared promo-code evaluator.</param>
public sealed class EvaluatePromoCodeActivity(PromoCodeEvaluator evaluator)
    : WorkflowActivity<EvaluatePromoCodeInput, PromoCodeEvaluation>
{
    /// <inheritdoc />
    public override Task<PromoCodeEvaluation> RunAsync(
        WorkflowActivityContext context,
        EvaluatePromoCodeInput input) =>
        evaluator.EvaluateAsync(
            input.CatalogEventId,
            input.Code,
            input.UserId,
            input.Lines,
            DateTimeOffset.UtcNow,
            CancellationToken.None);
}
