using FluentValidation;

namespace Catalog.Application.Features.CreateEvent;

/// <summary>Validation rules for <see cref="CreateEventCommand"/>.</summary>
public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateEventValidator()
    {
        RuleFor(command => command.VenueId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Currency).NotEmpty().Length(3);
        RuleFor(command => command.StartsAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}
