namespace Ordering.Application.Checkout;

/// <summary>Why a promo code could not be applied.</summary>
/// <remarks>
/// Distinct reasons rather than one "invalid": a buyer who mistyped a code, one whose code expired
/// yesterday, and one who has already used their allowance all need different things told to them,
/// and lumping them together produces the "it just says invalid" support ticket.
/// </remarks>
public enum PromoCodeRejection
{
    /// <summary>The event has no code with that text.</summary>
    NotFound,

    /// <summary>The organizer has retired the code.</summary>
    Inactive,

    /// <summary>The code's validity window has not opened yet.</summary>
    NotYetValid,

    /// <summary>The code's validity window has closed.</summary>
    Expired,

    /// <summary>The code has been redeemed as many times as it allows, in total.</summary>
    RedemptionLimitReached,

    /// <summary>This buyer has redeemed the code as many times as they are allowed.</summary>
    BuyerLimitReached,

    /// <summary>The code applies only to price tiers this order contains none of.</summary>
    NotApplicableToSelection,
}
