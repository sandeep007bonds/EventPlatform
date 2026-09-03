namespace Catalog.Application.Features.AddEventSession;

/// <summary>Validation rules for <see cref="AddEventSessionCommand"/>.</summary>
public sealed class AddEventSessionValidator : AbstractValidator<AddEventSessionCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public AddEventSessionValidator()
    {
        RuleFor(command => command.Name).MaximumLength(100);
        RuleFor(command => command.EndsAt).GreaterThan(command => command.StartsAt);
    }
}
