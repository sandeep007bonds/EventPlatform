namespace Catalog.Application.Features.ListEventGroups;

/// <summary>Validation rules for <see cref="ListEventGroupsQuery"/>.</summary>
public sealed class ListEventGroupsValidator : AbstractValidator<ListEventGroupsQuery>
{
    /// <summary>Initializes the validation rules.</summary>
    public ListEventGroupsValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
