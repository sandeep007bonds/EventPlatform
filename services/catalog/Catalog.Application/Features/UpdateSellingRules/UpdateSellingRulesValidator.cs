namespace Catalog.Application.Features.UpdateSellingRules;

/// <summary>Validation rules for <see cref="UpdateSellingRulesCommand"/>.</summary>
public sealed class UpdateSellingRulesValidator : AbstractValidator<UpdateSellingRulesCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public UpdateSellingRulesValidator()
    {
        RuleFor(command => command.MaxTicketsPerBuyer).GreaterThan(0).When(c => c.MaxTicketsPerBuyer is not null);
        RuleFor(command => command.TaxRatePercent).InclusiveBetween(0, 100).When(c => c.TaxRatePercent is not null);
        RuleFor(command => command.TaxLabel).MaximumLength(50);
        RuleFor(command => command.BookingFeePerTicketMinor).GreaterThanOrEqualTo(0);
    }
}
