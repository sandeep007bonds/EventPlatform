namespace Catalog.Application.Features.ChangeEventSlug;

/// <summary>Handles <see cref="ChangeEventSlugCommand"/> by repointing a draft event's public URL.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class ChangeEventSlugHandler(IEventRepository repository)
    : IRequestHandler<ChangeEventSlugCommand, ChangeEventSlugOutcome>
{
    /// <inheritdoc />
    public async Task<ChangeEventSlugOutcome> Handle(ChangeEventSlugCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return ChangeEventSlugOutcome.NotFound;
        }

        if (@event.Status != EventStatus.Draft)
        {
            return ChangeEventSlugOutcome.NotDraft;
        }

        // Normalized rather than rejected outright: an organizer typing "Coldplay Mumbai" into a
        // URL field means the obvious thing, and refusing it teaches them nothing they want to know.
        var slug = EventSlug.Basis(request.Slug);
        if (slug == @event.Slug)
        {
            return ChangeEventSlugOutcome.Changed;
        }

        var existing = await repository.GetBySlugAsync(slug, cancellationToken);
        if (existing is not null)
        {
            // Reported rather than silently suffixed, unlike creation: someone who typed this URL
            // wants *that* URL, and quietly handing them "coldplay-mumbai-2" is worse than saying no.
            return ChangeEventSlugOutcome.SlugTaken;
        }

        @event.ChangeSlug(slug);
        await repository.SaveChangesAsync(cancellationToken);
        return ChangeEventSlugOutcome.Changed;
    }
}
