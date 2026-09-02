namespace Venues.Application.Features.AddVenueFacility;

/// <summary>Validation rules for <see cref="AddVenueFacilityCommand"/>.</summary>
public sealed class AddVenueFacilityValidator : AbstractValidator<AddVenueFacilityCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public AddVenueFacilityValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Description).MaximumLength(500);
    }
}
