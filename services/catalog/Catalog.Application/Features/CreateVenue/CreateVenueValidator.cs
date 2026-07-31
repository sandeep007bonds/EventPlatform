namespace Catalog.Application.Features.CreateVenue;

/// <summary>Validation rules for <see cref="CreateVenueCommand"/>.</summary>
public sealed class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateVenueValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine2).MaximumLength(200);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Region).MaximumLength(100);
        RuleFor(command => command.PostalCode).MaximumLength(20);
        RuleFor(command => command.Country).NotEmpty().Length(2);
        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90).When(c => c.Latitude is not null);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180).When(c => c.Longitude is not null);
        RuleFor(command => command.Capacity).GreaterThan(0).When(c => c.Capacity is not null);
    }
}
