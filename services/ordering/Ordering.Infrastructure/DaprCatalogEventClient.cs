namespace Ordering.Infrastructure;

/// <summary>
/// Reads Catalog (app-id <c>catalog</c>) over Dapr service invocation, behind the
/// <see cref="ICatalogEventClient"/> port used by the checkout saga. Mirrors
/// <c>Ticketing.Infrastructure/DaprCatalogEventClient.cs</c>'s pattern.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprCatalogEventClient(DaprClient daprClient) : ICatalogEventClient
{
    private const string CatalogAppId = "catalog";

    /// <inheritdoc />
    public async Task<string?> GetEventCurrencyAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var pricing = await daprClient.InvokeMethodAsync<CatalogEventPricing>(
            HttpMethod.Get,
            CatalogAppId,
            $"v1/events/{eventId}",
            cancellationToken);

        return string.IsNullOrWhiteSpace(pricing?.Currency) ? null : pricing.Currency;
    }
}
