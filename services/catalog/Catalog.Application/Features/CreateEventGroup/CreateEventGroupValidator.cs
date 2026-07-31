namespace Catalog.Application.Features.CreateEventGroup;

/// <summary>Validation rules for <see cref="CreateEventGroupCommand"/>.</summary>
public sealed class CreateEventGroupValidator : AbstractValidator<CreateEventGroupCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateEventGroupValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
    }
}
