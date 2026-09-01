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
        RuleFor(command => command.EndsAt).GreaterThan(command => command.StartsAt);

        RuleFor(command => command.LocationName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine1).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine2).MaximumLength(200);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Region).MaximumLength(100);
        RuleFor(command => command.PostalCode).MaximumLength(20);
        RuleFor(command => command.Country).NotEmpty().Length(2);
        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90).When(c => c.Latitude is not null);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180).When(c => c.Longitude is not null);
        RuleFor(command => command.MaxTicketsPerBuyer).GreaterThan(0).When(c => c.MaxTicketsPerBuyer is not null);
        RuleFor(command => command.TaxRatePercent).InclusiveBetween(0, 100).When(c => c.TaxRatePercent is not null);
        RuleFor(command => command.TaxLabel).MaximumLength(50);
        RuleFor(command => command.BookingFeePerTicketMinor).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Slug).MaximumLength(EventSlug.MaxLength);
        RuleFor(command => command.TimeZoneId).MaximumLength(100);
        RuleFor(command => command.TimeZoneId)
            .Must(BeAKnownTimeZone)
            .When(c => !string.IsNullOrWhiteSpace(c.TimeZoneId))
            .WithMessage("'{PropertyName}' must be a known IANA time zone, e.g. 'Asia/Kolkata'.");
    }

    // Checked here rather than in the domain: resolving an id depends on the host's time-zone
    // database, and an aggregate whose invariants vary with the machine it runs on is not an
    // invariant. The API rejects an unknown zone with a clear message; Event itself only requires
    // that the string be a plausible length.
    private static bool BeAKnownTimeZone(string? timeZoneId) =>
        timeZoneId is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
