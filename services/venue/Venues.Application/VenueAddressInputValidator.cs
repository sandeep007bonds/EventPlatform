namespace Venues.Application;

/// <summary>Validation rules for <see cref="VenueAddressInput"/>, shared by create and update.</summary>
public sealed class VenueAddressInputValidator : AbstractValidator<VenueAddressInput>
{
    /// <summary>Initializes the validation rules.</summary>
    public VenueAddressInputValidator()
    {
        RuleFor(address => address.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(address => address.AddressLine2).MaximumLength(200);
        RuleFor(address => address.City).NotEmpty().MaximumLength(100);
        RuleFor(address => address.Region).MaximumLength(100);
        RuleFor(address => address.PostalCode).MaximumLength(20);
        RuleFor(address => address.Country).NotEmpty().Length(2);
        RuleFor(address => address.Latitude).InclusiveBetween(-90d, 90d).When(address => address.Latitude is not null);
        RuleFor(address => address.Longitude).InclusiveBetween(-180d, 180d).When(address => address.Longitude is not null);
    }
}
