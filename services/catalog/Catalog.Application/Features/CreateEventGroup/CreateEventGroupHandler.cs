namespace Catalog.Application.Features.CreateEventGroup;

/// <summary>Handles <see cref="CreateEventGroupCommand"/> by creating and persisting an event group.</summary>
/// <param name="repository">The event group repository.</param>
internal sealed class CreateEventGroupHandler(IEventGroupRepository repository)
    : IRequestHandler<CreateEventGroupCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CreateEventGroupCommand request, CancellationToken cancellationToken)
    {
        var group = EventGroup.Create(request.TenantId, request.Title);

        repository.Add(group);
        await repository.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}
