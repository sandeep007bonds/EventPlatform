namespace Venues.Application.Features.CreateVenue;

/// <summary>Validation rules for <see cref="CreateVenueCommand"/>.</summary>
public sealed class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateVenueValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.VenueType).MaximumLength(100);
        RuleFor(command => command.TimeZoneId).MaximumLength(100);
        RuleFor(command => command.Address).NotNull().SetValidator(new VenueAddressInputValidator());
    }
}
