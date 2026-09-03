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
        RuleFor(command => command.MaxTicketsPerBuyer).GreaterThan(0).When(c => c.MaxTicketsPerBuyer is not null);
        RuleFor(command => command.TaxRatePercent).InclusiveBetween(0, 100).When(c => c.TaxRatePercent is not null);
        RuleFor(command => command.TaxLabel).MaximumLength(50);
        RuleFor(command => command.BookingFeePerTicketMinor).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Slug).MaximumLength(EventSlug.MaxLength);
    }
}
