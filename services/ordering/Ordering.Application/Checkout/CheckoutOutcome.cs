namespace Ordering.Application.Checkout;

/// <summary>Result of a checkout attempt.</summary>
public enum CheckoutOutcome
{
    /// <summary>Payment captured and seats sold — the order is confirmed.</summary>
    Confirmed,

    /// <summary>The hold does not exist.</summary>
    HoldNotFound,

    /// <summary>The hold belongs to another user.</summary>
    Forbidden,

    /// <summary>The hold is not active.</summary>
    HoldNotActive,

    /// <summary>The hold has expired.</summary>
    HoldExpired,

    /// <summary>Payment failed; the hold was released.</summary>
    PaymentFailed,

    /// <summary>The seats could not be sold; payment was refunded and the hold released.</summary>
    ConvertFailed,

    /// <summary>A prior attempt for this idempotency key ended in failure.</summary>
    Failed,
}
