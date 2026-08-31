namespace Catalog.Application.Features.CreateTicketType;

/// <summary>Validation rules for <see cref="CreateTicketTypeCommand"/>.</summary>
public sealed class CreateTicketTypeValidator : AbstractValidator<CreateTicketTypeCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public CreateTicketTypeValidator()
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
