namespace Catalog.Application.Features.UpdateTicketType;

/// <summary>Validation rules for <see cref="UpdateTicketTypeCommand"/>.</summary>
public sealed class UpdateTicketTypeValidator : AbstractValidator<UpdateTicketTypeCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateTicketTypeValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PriceMinor).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.MaxPerBuyer).GreaterThan(0).When(c => c.MaxPerBuyer is not null);
        RuleFor(command => command.SalesEndsAt)
            .GreaterThan(command => command.SalesStartsAt)
            .When(c => c.SalesStartsAt is not null && c.SalesEndsAt is not null);
    }
}
