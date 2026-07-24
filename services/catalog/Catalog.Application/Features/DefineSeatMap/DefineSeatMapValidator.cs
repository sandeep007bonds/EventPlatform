namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>Validation rules for <see cref="DefineSeatMapCommand"/>.</summary>
public sealed class DefineSeatMapValidator : AbstractValidator<DefineSeatMapCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public DefineSeatMapValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Sections).NotEmpty();

        RuleForEach(command => command.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.Name).NotEmpty().MaximumLength(100);
            section.RuleFor(s => s.PriceTier).NotEmpty().MaximumLength(50);
            section.RuleFor(s => s.PriceAmount).GreaterThanOrEqualTo(0m);
            section.RuleFor(s => s.Rows).InclusiveBetween(1, 500);
            section.RuleFor(s => s.SeatsPerRow).InclusiveBetween(1, 500);
        });
    }
}
