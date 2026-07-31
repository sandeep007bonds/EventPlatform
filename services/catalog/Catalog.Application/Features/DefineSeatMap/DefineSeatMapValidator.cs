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

            section.RuleFor(s => s.Rows).NotNull().InclusiveBetween(1, 500)
                .When(s => s.AllocationType == AllocationType.Reserved);
            section.RuleFor(s => s.SeatsPerRow).NotNull().InclusiveBetween(1, 500)
                .When(s => s.AllocationType == AllocationType.Reserved);
            section.RuleFor(s => s.Capacity).Null()
                .When(s => s.AllocationType == AllocationType.Reserved);

            section.RuleFor(s => s.Capacity).NotNull().InclusiveBetween(1, 1_000_000)
                .When(s => s.AllocationType == AllocationType.GeneralAdmission);
            section.RuleFor(s => s.Rows).Null()
                .When(s => s.AllocationType == AllocationType.GeneralAdmission);
            section.RuleFor(s => s.SeatsPerRow).Null()
                .When(s => s.AllocationType == AllocationType.GeneralAdmission);
        });
    }
}
