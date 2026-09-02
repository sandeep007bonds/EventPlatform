namespace Venues.Application.Features.UpdateVenue;

/// <summary>Validation rules for <see cref="UpdateVenueCommand"/>.</summary>
public sealed class UpdateVenueValidator : AbstractValidator<UpdateVenueCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateVenueValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.VenueType).MaximumLength(100);
        RuleFor(command => command.TimeZoneId).MaximumLength(100);
        RuleFor(command => command.Address).NotNull().SetValidator(new VenueAddressInputValidator());
    }
}
