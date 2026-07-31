namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>Validation rules for <see cref="UpdateEventDetailsCommand"/>.</summary>
public sealed class UpdateEventDetailsValidator : AbstractValidator<UpdateEventDetailsCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateEventDetailsValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Description).MaximumLength(4000);
        RuleFor(command => command.Category).MaximumLength(100);
        RuleFor(command => command.AgeRestriction).MaximumLength(50);
        RuleFor(command => command.BannerImageUrl).MaximumLength(2000);
        RuleFor(command => command.VideoUrl).MaximumLength(2000);
        RuleFor(command => command.OffSaleAt)
            .GreaterThan(command => command.OnSaleAt)
            .When(command => command.OnSaleAt is not null && command.OffSaleAt is not null)
            .WithMessage("OffSaleAt must be after OnSaleAt.");
    }
}
