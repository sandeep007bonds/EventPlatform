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

            // A priced block needs a type and an excluded one has none to give. The aggregate
            // refuses both-or-neither too; this is the same rule said early, as a 400 naming the
            // field rather than a 409 naming the block.
            allocation.RuleFor(a => a.TicketTypeId).NotEmpty().When(a => !a.IsExcluded);
            allocation.RuleFor(a => a.TicketTypeId).Null().When(a => a.IsExcluded);

            allocation.RuleFor(a => a.DisplayName).MaximumLength(100);
            allocation.RuleFor(a => a.CapacityOverride).GreaterThan(0).When(a => a.CapacityOverride is not null);
        });
    }
}
