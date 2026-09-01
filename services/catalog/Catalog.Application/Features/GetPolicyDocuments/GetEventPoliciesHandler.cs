namespace Catalog.Application.Features.GetPolicyDocuments;

/// <summary>
/// Handles <see cref="GetEventPoliciesQuery"/> by resolving the event's owner, then applying the
/// same override-wins rule as <see cref="GetPolicyDocumentsHandler"/>.
/// </summary>
/// <param name="events">The event repository.</param>
/// <param name="sender">The mediator, to reuse the resolution logic rather than restate it.</param>
internal sealed class GetEventPoliciesHandler(IEventRepository events, ISender sender)
    : IRequestHandler<GetEventPoliciesQuery, IReadOnlyList<PolicyDocumentResponse>?>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDocumentResponse>?> Handle(
        GetEventPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await events.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || !@event.IsVisibleTo(request.CallerTenantId))
        {
            return null;
        }

        return await sender.Send(new GetPolicyDocumentsQuery(@event.TenantId, @event.Id), cancellationToken);
    }
}
