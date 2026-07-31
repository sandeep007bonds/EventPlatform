namespace Catalog.Application.Features.ListVenues;

/// <summary>Validation rules for <see cref="ListVenuesQuery"/>.</summary>
public sealed class ListVenuesValidator : AbstractValidator<ListVenuesQuery>
{
    /// <summary>Initializes the validation rules.</summary>
    public ListVenuesValidator()
    {
        RuleFor(query => query.TenantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
