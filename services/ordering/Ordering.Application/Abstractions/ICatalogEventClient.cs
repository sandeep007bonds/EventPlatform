namespace Ordering.Application.Abstractions;

/// <summary>Reads the Catalog service for details the checkout saga needs to price an order.</summary>
public interface ICatalogEventClient
{
    /// <summary>
    /// Reads the event's pricing facts: the currency the organizer authored it in, and the tax rate
    /// to apply. Currency matters beyond display — a payment provider's available methods are
    /// currency-dependent (e.g. Stripe only offers UPI on INR charges).
    /// </summary>
    /// <param name="eventId">The show/event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The pricing facts, or <see langword="null"/> if the event can't be read.</returns>
    Task<EventPricing?> GetEventPricingAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>
    /// Looks up one of an event's promo codes by the string the buyer typed. Case-insensitive.
    /// </summary>
    /// <param name="eventId">The event the code belongs to.</param>
    /// <param name="code">The code as typed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The code's rules, or <see langword="null"/> if the event has no such code.</returns>
    Task<PromoCodeDefinition?> GetPromoCodeAsync(Guid eventId, string code, CancellationToken cancellationToken);
}
