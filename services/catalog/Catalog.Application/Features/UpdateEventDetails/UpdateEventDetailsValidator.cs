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
        RuleFor(command => command.BookingEndsAt)
            .GreaterThan(command => command.OnSaleAt)
            .When(command => command.OnSaleAt is not null && command.BookingEndsAt is not null)
            .WithMessage("BookingEndsAt must be after OnSaleAt.");

        RuleFor(command => command.MaxTicketsPerBuyer)
            .GreaterThan(0)
            .When(command => command.MaxTicketsPerBuyer is not null);

        RuleFor(command => command.ContactPhone).MaximumLength(30);
        RuleFor(command => command.ContactMobile).MaximumLength(30);
        RuleFor(command => command.ContactEmail).MaximumLength(200).EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.ContactEmail));
        RuleFor(command => command.WebsiteUrl).MaximumLength(2000);

        RuleForEach(command => command.SocialLinks).ChildRules(link =>
        {
            link.RuleFor(l => l.Platform).NotEmpty().MaximumLength(50);
            link.RuleFor(l => l.Url).NotEmpty().MaximumLength(2000);
        });
    }
}
