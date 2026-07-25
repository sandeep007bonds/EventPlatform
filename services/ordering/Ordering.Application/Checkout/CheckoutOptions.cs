namespace Ordering.Application.Checkout;

/// <summary>Options for checkout.</summary>
public sealed class CheckoutOptions
{
    /// <summary>
    /// Currency used when pricing an order. Defaults to <c>USD</c>. See tracker T-currency: this
    /// should be derived from the Catalog event rather than assumed.
    /// </summary>
    public string DefaultCurrency { get; set; } = "USD";
}
