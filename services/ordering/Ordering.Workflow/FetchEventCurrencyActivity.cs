namespace Ordering.Workflow;

/// <summary>
/// Reads the event's pricing currency from Catalog, so the order is priced in the currency the
/// organizer authored rather than a platform-wide default. Falls back to
/// <see cref="CheckoutOptions.DefaultCurrency"/> when Catalog can't be read, so an unreachable
/// Catalog degrades to the previous behavior instead of failing the checkout outright.
/// </summary>
/// <param name="catalog">The Catalog client.</param>
/// <param name="options">Checkout options (the fallback currency).</param>
public sealed class FetchEventCurrencyActivity(ICatalogEventClient catalog, CheckoutOptions options)
    : WorkflowActivity<Guid, string>
{
    /// <inheritdoc />
    public override async Task<string> RunAsync(WorkflowActivityContext context, Guid catalogEventId)
    {
        var currency = await catalog.GetEventCurrencyAsync(catalogEventId, CancellationToken.None);
        return string.IsNullOrWhiteSpace(currency) ? options.DefaultCurrency : currency;
    }
}
