namespace Catalog.Application.Features.CreateEvent;

/// <summary>Validation rules for <see cref="CreateEventCommand"/>.</summary>
public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateEventValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.StartsAt).GreaterThan(DateTimeOffset.UtcNow);

        RuleFor(command => command.LocationName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine2).MaximumLength(200);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Region).MaximumLength(100);
        RuleFor(command => command.PostalCode).MaximumLength(20);
        RuleFor(command => command.Country).NotEmpty().Length(2);
        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90).When(c => c.Latitude is not null);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180).When(c => c.Longitude is not null);
    }
}
