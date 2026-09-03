namespace Catalog.Application.Features.UpdateEventSession;

/// <summary>Validation rules for <see cref="UpdateEventSessionCommand"/>.</summary>
public sealed class UpdateEventSessionValidator : AbstractValidator<UpdateEventSessionCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateEventSessionValidator()
    {
        RuleFor(command => command.Name).MaximumLength(100);
        RuleFor(command => command.EndsAt).GreaterThan(command => command.StartsAt);
    }
}
