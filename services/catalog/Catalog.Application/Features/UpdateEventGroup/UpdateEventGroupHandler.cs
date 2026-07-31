namespace Catalog.Application.Features.UpdateEventGroup;

/// <summary>Handles <see cref="UpdateEventGroupCommand"/> by updating an event group in place.</summary>
/// <param name="repository">The event group repository.</param>
internal sealed class UpdateEventGroupHandler(IEventGroupRepository repository)
    : IRequestHandler<UpdateEventGroupCommand, UpdateEventGroupOutcome>
{
    /// <inheritdoc />
    public async Task<UpdateEventGroupOutcome> Handle(UpdateEventGroupCommand request, CancellationToken cancellationToken)
    {
        var group = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (group is null || group.TenantId != request.TenantId)
        {
            return UpdateEventGroupOutcome.NotFound;
        }

        group.Update(
            request.Title,
            request.StartsAt,
            request.EndsAt,
            request.ContactPhone,
            request.ContactMobile,
            request.ContactEmail,
            request.WebsiteUrl,
            request.SocialLinks.Select(link => (link.Platform, link.Url)));

        await repository.SaveChangesAsync(cancellationToken);
        return UpdateEventGroupOutcome.Updated;
    }
}
