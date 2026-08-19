namespace Ordering.Workflow;

/// <summary>
/// Reads the event's pricing facts from Catalog: the currency the organizer authored it in, and the
/// tax rate to apply. Currency matters beyond display — the payment provider's available methods
/// depend on it (Stripe only offers UPI on INR charges).
/// </summary>
/// <remarks>
/// Falls back to <see cref="CheckoutOptions.DefaultCurrency"/> and no tax when Catalog can't be
/// read, so an unreachable Catalog degrades rather than failing the checkout outright. Charging
/// *no* tax on a fallback is the conservative direction: under-collecting is a reconcilable
/// accounting problem, while over-charging a buyer who never agreed to it is not.
/// </remarks>
/// <param name="catalog">The Catalog client.</param>
/// <param name="options">Checkout options (the fallback currency).</param>
public sealed class FetchEventPricingActivity(ICatalogEventClient catalog, CheckoutOptions options)
    : WorkflowActivity<Guid, EventPricing>
{
    /// <inheritdoc />
    public override async Task<EventPricing> RunAsync(WorkflowActivityContext context, Guid catalogEventId)
    {
        var pricing = await catalog.GetEventPricingAsync(catalogEventId, CancellationToken.None);

        return pricing is null || string.IsNullOrWhiteSpace(pricing.Currency)
            ? new EventPricing(options.DefaultCurrency, null, null)
            : pricing;
    }
}
