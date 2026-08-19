namespace Catalog.Application.Features.CreatePromoCode;

/// <summary>Result of attempting to create a promo code for an event.</summary>
public enum CreatePromoCodeOutcome
{
    /// <summary>The promo code was created.</summary>
    Created,

    /// <summary>No matching event exists for the caller's tenant.</summary>
    EventNotFound,

    /// <summary>The event already has a code with this text.</summary>
    DuplicateCode,
}
