namespace Catalog.Application.Features.DeactivatePromoCode;

/// <summary>Result of attempting to retire a promo code.</summary>
public enum DeactivatePromoCodeOutcome
{
    /// <summary>The code is now inactive. Also returned when it already was — deactivation is idempotent.</summary>
    Deactivated,

    /// <summary>No matching promo code exists for the caller's tenant.</summary>
    NotFound,
}
