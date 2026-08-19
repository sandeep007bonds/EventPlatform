namespace Ordering.Infrastructure;

/// <summary>
/// Reads Catalog (app-id <c>catalog</c>) over Dapr service invocation, behind the
/// <see cref="ICatalogEventClient"/> port used by the checkout saga.
/// </summary>
/// <remarks>
/// Uses <c>CreateInvokeHttpClient</c> + explicit status checks rather than
/// <c>InvokeMethodAsync</c>, matching <see cref="DaprHoldClient"/>: a 404 here is an ordinary
/// outcome (an unknown event, a mistyped promo code), and turning ordinary outcomes into
/// exceptions to be caught reads worse than checking the status.
/// </remarks>
internal sealed class DaprCatalogEventClient : ICatalogEventClient
{
    private const string CatalogAppId = "catalog";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<EventPricing?> GetEventPricingAsync(Guid eventId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(CatalogAppId);
        using var response = await http.GetAsync($"v1/events/{eventId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var pricing = await response.Content.ReadFromJsonAsync<CatalogEventPricing>(JsonOptions, cancellationToken);

        return string.IsNullOrWhiteSpace(pricing?.Currency)
            ? null
            : new EventPricing(pricing.Currency, pricing.TaxRatePercent, pricing.TaxLabel);
    }

    /// <inheritdoc />
    public async Task<PromoCodeDefinition?> GetPromoCodeAsync(
        Guid eventId,
        string code,
        CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(CatalogAppId);
        using var response = await http.GetAsync(
            $"v1/events/{eventId}/promo-codes/by-code/{Uri.EscapeDataString(code)}",
            cancellationToken);

        // A code the buyer mistyped is the expected 404 here, not a fault. Any other failure
        // propagates: a Catalog outage must not quietly become "no discount" on an order the buyer
        // expected to be discounted.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var promoCode = await response.Content.ReadFromJsonAsync<CatalogPromoCode>(JsonOptions, cancellationToken);

        return promoCode is null
            ? null
            : new PromoCodeDefinition(
                promoCode.Id,
                promoCode.Code,
                promoCode.DiscountType,
                promoCode.DiscountValue,
                promoCode.ValidFrom,
                promoCode.ValidTo,
                promoCode.IsActive,
                promoCode.MaxRedemptions,
                promoCode.MaxRedemptionsPerBuyer,
                promoCode.PriceTiers ?? []);
    }
}
