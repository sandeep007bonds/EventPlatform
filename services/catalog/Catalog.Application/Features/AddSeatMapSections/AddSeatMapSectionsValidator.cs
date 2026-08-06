namespace Catalog.Application.Features.AddSeatMapSections;

/// <summary>Validation rules for <see cref="AddSeatMapSectionsCommand"/> — mirrors <see cref="DefineSeatMapValidator"/>'s per-section rules.</summary>
public sealed class AddSeatMapSectionsValidator : AbstractValidator<AddSeatMapSectionsCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public AddSeatMapSectionsValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
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
