namespace Catalog.Application.Features.CreateEntryGate;

/// <summary>Validation rules for <see cref="CreateEntryGateCommand"/>.</summary>
public sealed class CreateEntryGateValidator : AbstractValidator<CreateEntryGateCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateEntryGateValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
    }
}
