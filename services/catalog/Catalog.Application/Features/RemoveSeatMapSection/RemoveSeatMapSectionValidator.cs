namespace Catalog.Application.Features.RemoveSeatMapSection;

/// <summary>Validation rules for <see cref="RemoveSeatMapSectionCommand"/>.</summary>
public sealed class RemoveSeatMapSectionValidator : AbstractValidator<RemoveSeatMapSectionCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public RemoveSeatMapSectionValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.SectionName).NotEmpty();
    }
}
