namespace Venues.Application.Features.CreateSeatMap;

/// <summary>Validation rules for <see cref="CreateSeatMapCommand"/>.</summary>
public sealed class CreateSeatMapValidator : AbstractValidator<CreateSeatMapCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateSeatMapValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
    }
}
