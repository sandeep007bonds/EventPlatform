namespace Catalog.Application.Features.GetEventGroup;

/// <summary>Handles <see cref="GetEventGroupQuery"/>, mapping the aggregate to a read model.</summary>
/// <param name="repository">The event group repository.</param>
internal sealed class GetEventGroupHandler(IEventGroupRepository repository)
    : IRequestHandler<GetEventGroupQuery, EventGroupResponse?>
{
    /// <inheritdoc />
    public async Task<EventGroupResponse?> Handle(GetEventGroupQuery request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.Id, cancellationToken);

        return group is null ? null : new EventGroupResponse(group.Id, group.Title);
    }
}
