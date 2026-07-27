namespace Catalog.Application.Features.ListEvents;

/// <summary>Validation rules for <see cref="ListEventsQuery"/>.</summary>
public sealed class ListEventsValidator : AbstractValidator<ListEventsQuery>
{
    /// <summary>Initializes the validation rules.</summary>
    public ListEventsValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
