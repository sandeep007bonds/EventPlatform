namespace Catalog.Application.Features.UpdateEventPresentation;

/// <summary>
/// Handles <see cref="UpdateEventPresentationCommand"/> by setting an event's presentational
/// fields and enqueuing an <see cref="EventUpdated"/> integration event in the same unit of work.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class UpdateEventPresentationHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<UpdateEventPresentationCommand, UpdateEventPresentationOutcome>
{
    /// <inheritdoc />
    public async Task<UpdateEventPresentationOutcome> Handle(
        UpdateEventPresentationCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return UpdateEventPresentationOutcome.NotFound;
        }

        @event.UpdatePresentation(
            request.Title,
            request.Description,
            request.Category,
            request.AgeRestriction,
            request.BannerImageUrl,
            request.VideoUrl,
            request.ContactPhone,
            request.ContactMobile,
            request.ContactEmail,
            request.WebsiteUrl,
            request.SocialLinks.Select(link => (link.Platform, link.Url)));

        events.Enqueue(new EventUpdated(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id));

        await repository.SaveChangesAsync(cancellationToken);
        return UpdateEventPresentationOutcome.Updated;
    }
}
