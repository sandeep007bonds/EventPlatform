namespace Catalog.Application.Features.CreatePromoCode;

/// <summary>Outcome of a <see cref="CreatePromoCodeCommand"/>, with the new code's id when created.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="PromoCodeId">The new code's id when created; otherwise <see langword="null"/>.</param>
public sealed record CreatePromoCodeResult(CreatePromoCodeOutcome Outcome, Guid? PromoCodeId);
