namespace Catalog.Application.Features.CreatePromoCode;

/// <summary>Validation rules for <see cref="CreatePromoCodeCommand"/>.</summary>
public sealed class CreatePromoCodeValidator : AbstractValidator<CreatePromoCodeCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreatePromoCodeValidator()
    {
        // No whitespace: the code is typed by a buyer and compared exactly, and a leading or
        // trailing space (or one in the middle) is invisible in the organizer's form and turns
        // into an unexplainable "code not found" for everyone they gave it to.
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("A promo code may only contain letters, digits, hyphens and underscores.");

        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.DiscountType).IsInEnum();

        RuleFor(command => command.DiscountValue)
            .InclusiveBetween(0.01m, 100m)
            .When(command => command.DiscountType == DiscountType.Percentage)
            .WithMessage("A percentage discount must be between 0.01 and 100.");

        RuleFor(command => command.DiscountValue)
            .GreaterThan(0m)
            .When(command => command.DiscountType == DiscountType.FixedAmount)
            .WithMessage("A fixed discount must be greater than zero.");

        RuleFor(command => command.ValidTo)
            .GreaterThan(command => command.ValidFrom)
            .When(command => command.ValidFrom is not null && command.ValidTo is not null)
            .WithMessage("ValidTo must be after ValidFrom.");

        RuleFor(command => command.MaxRedemptions)
            .GreaterThan(0)
            .When(command => command.MaxRedemptions is not null);

        RuleFor(command => command.MaxRedemptionsPerBuyer)
            .GreaterThan(0)
            .When(command => command.MaxRedemptionsPerBuyer is not null);

        RuleForEach(command => command.PriceTiers).NotEmpty().MaximumLength(50);
    }
}
