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

    /// <summary>Payment failed (declined, or authentication abandoned); the hold was released.</summary>
    PaymentFailed,

    /// <summary>
    /// The buyer never completed payment authentication (3-D Secure challenge, UPI app-switch, etc.)
    /// before the extended hold's deadline; the hold was released. Distinct from
    /// <see cref="PaymentFailed"/> so ops/frontend can tell "never finished authenticating" apart
    /// from "the provider declined it."
    /// </summary>
    PaymentTimedOut,

    /// <summary>The seats could not be sold; payment was refunded and the hold released.</summary>
    ConvertFailed,

    /// <summary>A prior attempt for this idempotency key ended in failure.</summary>
    Failed,

    /// <summary>
    /// A concurrent checkout for the same idempotency key won the race to create the order; this
    /// attempt did nothing (no double charge). The caller should re-fetch or retry the key.
    /// </summary>
    Duplicate,
}
