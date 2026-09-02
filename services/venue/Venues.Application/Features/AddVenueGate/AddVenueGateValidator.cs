namespace Venues.Application.Features.AddVenueGate;

/// <summary>Validation rules for <see cref="AddVenueGateCommand"/>.</summary>
public sealed class AddVenueGateValidator : AbstractValidator<AddVenueGateCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public AddVenueGateValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
    }
}
