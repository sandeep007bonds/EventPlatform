namespace Ordering.Application.Checkout;

/// <summary>
/// The result of checking whether a promo code can be applied to a specific set of order lines by
/// a specific buyer.
/// </summary>
/// <param name="Terms">The code's arithmetic terms when accepted; <see langword="null"/> when rejected.</param>
/// <param name="PromoCodeId">The code's Catalog id when accepted.</param>
/// <param name="Code">The code as stored, when accepted.</param>
/// <param name="Rejection">Why it was rejected; <see langword="null"/> when accepted.</param>
public sealed record PromoCodeEvaluation(
    PromoCodeTerms? Terms,
    Guid? PromoCodeId,
    string? Code,
    PromoCodeRejection? Rejection)
{
    /// <summary>Whether the code may be applied.</summary>
    public bool IsAccepted => Terms is not null;

    /// <summary>Builds an accepted evaluation.</summary>
    /// <param name="terms">The code's arithmetic terms.</param>
    /// <param name="promoCodeId">The code's Catalog id.</param>
    /// <param name="code">The code as stored.</param>
    /// <returns>An accepted evaluation.</returns>
    public static PromoCodeEvaluation Accepted(PromoCodeTerms terms, Guid promoCodeId, string code) =>
        new(terms, promoCodeId, code, null);

    /// <summary>Builds a rejected evaluation.</summary>
    /// <param name="rejection">Why the code was rejected.</param>
    /// <returns>A rejected evaluation.</returns>
    public static PromoCodeEvaluation Rejected(PromoCodeRejection rejection) =>
        new(null, null, null, rejection);
}
