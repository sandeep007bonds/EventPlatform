namespace Catalog.Application.Features.UpdateEventGroup;

/// <summary>Validation rules for <see cref="UpdateEventGroupCommand"/>.</summary>
public sealed class UpdateEventGroupValidator : AbstractValidator<UpdateEventGroupCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateEventGroupValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.EndsAt)
            .GreaterThan(command => command.StartsAt)
            .When(command => command.StartsAt is not null && command.EndsAt is not null);

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
