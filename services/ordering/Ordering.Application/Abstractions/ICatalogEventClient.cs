namespace Ordering.Application.Abstractions;

/// <summary>Reads the Catalog service for details the checkout saga needs to price an order.</summary>
public interface ICatalogEventClient
{
    /// <summary>
    /// Reads the event's pricing currency, so an order is priced in the currency the organizer
    /// actually authored the event in rather than a platform-wide default. This matters beyond
    /// display: a payment provider's available methods are currency-dependent (e.g. Stripe only
    /// offers UPI on INR charges).
    /// </summary>
    /// <param name="eventId">The show/event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The ISO 4217 currency code, or <see langword="null"/> if the event can't be read.</returns>
    Task<string?> GetEventCurrencyAsync(Guid eventId, CancellationToken cancellationToken);
}
