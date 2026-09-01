namespace Catalog.Application.Features.ChangeEventSlug;

/// <summary>Validation rules for <see cref="ChangeEventSlugCommand"/>.</summary>
public sealed class ChangeEventSlugValidator : AbstractValidator<ChangeEventSlugCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public ChangeEventSlugValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(EventSlug.MaxLength)
            .Must(NormalizeToSomethingUsable)
            .When(command => !string.IsNullOrWhiteSpace(command.Slug))
            .WithMessage("'{PropertyName}' must contain letters or digits, and must not be a reserved word.");
    }

    // The handler normalizes what it is given, so almost anything is acceptable. The two cases that
    // are not are the ones `EventSlug.Basis` rescues with its "e" fallback: a value with no
    // alphanumerics at all ("!!!"), and a reserved word. Silently turning either into "e" would
    // give an organizer a URL they did not ask for, so they are refused here instead.
    private static bool NormalizeToSomethingUsable(string slug) =>
        EventSlug.Basis(slug) != "e" || EventSlug.IsValid(slug);
}
