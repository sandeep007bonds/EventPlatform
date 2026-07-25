namespace Ordering.Application.Checkout;

/// <summary>Outcome of a checkout, with the order id when one was created.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="OrderId">The order id, when one exists; otherwise <see langword="null"/>.</param>
public sealed record CheckoutResult(CheckoutOutcome Outcome, Guid? OrderId)
{
    /// <summary>A confirmed checkout.</summary>
    /// <param name="orderId">The confirmed order id.</param>
    /// <returns>A confirmed result.</returns>
    public static CheckoutResult Confirmed(Guid orderId) => new(CheckoutOutcome.Confirmed, orderId);

    /// <summary>A failed checkout.</summary>
    /// <param name="outcome">The failure outcome.</param>
    /// <param name="orderId">The order id, if one was created.</param>
    /// <returns>A failed result.</returns>
    public static CheckoutResult Failed(CheckoutOutcome outcome, Guid? orderId = null) => new(outcome, orderId);
}
