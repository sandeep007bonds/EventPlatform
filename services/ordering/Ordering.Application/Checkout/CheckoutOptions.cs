namespace Ordering.Application.Checkout;

/// <summary>Options for checkout.</summary>
public sealed class CheckoutOptions
{
    /// <summary>
    /// Fallback currency used when pricing an order, only when the Catalog event's own currency
    /// can't be read (see <c>FetchEventCurrencyActivity</c>). An order is normally priced in the
    /// currency the organizer authored the event in. Defaults to <c>USD</c>.
    /// </summary>
    public string DefaultCurrency { get; set; } = "USD";
}
