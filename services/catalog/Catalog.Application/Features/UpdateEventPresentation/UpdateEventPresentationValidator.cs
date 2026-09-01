namespace Catalog.Application.Features.UpdateEventPresentation;

/// <summary>Validation rules for <see cref="UpdateEventPresentationCommand"/>.</summary>
public sealed class UpdateEventPresentationValidator : AbstractValidator<UpdateEventPresentationCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateEventPresentationValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Description).MaximumLength(4000);
        RuleFor(command => command.Category).MaximumLength(100);
        RuleFor(command => command.AgeRestriction).MaximumLength(50);
        RuleFor(command => command.BannerImageUrl).MaximumLength(2000);
        RuleFor(command => command.VideoUrl).MaximumLength(2000);
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
