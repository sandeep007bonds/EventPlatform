namespace Catalog.Application.Features.SetSessionAllocations;

/// <summary>Validation rules for <see cref="SetSessionAllocationsCommand"/>.</summary>
public sealed class SetSessionAllocationsValidator : AbstractValidator<SetSessionAllocationsCommand>
{
    /// <summary>Initializes the validation rules.</summary>
    public SetSessionAllocationsValidator()
    {
        RuleFor(command => command.Allocations).NotNull();
        RuleForEach(command => command.Allocations).ChildRules(allocation =>
        {
            allocation.RuleFor(a => a.Code).NotEmpty().MaximumLength(32);
            allocation.RuleFor(a => a.TicketTypeId).NotEmpty();
        });
    }
}
