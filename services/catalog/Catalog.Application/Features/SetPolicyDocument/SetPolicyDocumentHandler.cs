namespace Catalog.Application.Features.SetPolicyDocument;

/// <summary>
/// Handles <see cref="SetPolicyDocumentCommand"/> by sanitising the submitted HTML and either
/// creating the document or revising it in place.
/// </summary>
/// <param name="repository">The policy-document repository.</param>
/// <param name="events">The event repository, to check ownership of an event-scoped document.</param>
/// <param name="sanitizer">Strips anything executable from the submitted HTML.</param>
internal sealed class SetPolicyDocumentHandler(
    IPolicyDocumentRepository repository,
    IEventRepository events,
    IHtmlSanitizer sanitizer)
    : IRequestHandler<SetPolicyDocumentCommand, SetPolicyDocumentResult>
{
    /// <inheritdoc />
    public async Task<SetPolicyDocumentResult> Handle(SetPolicyDocumentCommand request, CancellationToken cancellationToken)
    {
        if (request.EventId is { } eventId)
        {
            var @event = await events.GetByIdAsync(eventId, cancellationToken);
            if (@event is null || @event.TenantId != request.TenantId)
            {
                return new SetPolicyDocumentResult(SetPolicyDocumentOutcome.EventNotFound, 0);
            }
        }

        // Sanitised here rather than at render time. Storing the cleaned text means a future reader
        // that forgets to escape cannot reach a payload that was never persisted — and it makes the
        // database itself the thing you can inspect to prove a `<script>` did not survive.
        var bodyHtml = sanitizer.Sanitize(request.BodyHtml).Trim();
        if (bodyHtml.Length == 0)
        {
            return new SetPolicyDocumentResult(SetPolicyDocumentOutcome.BodyEmptyAfterSanitising, 0);
        }

        var existing = await repository.GetAsync(request.TenantId, request.EventId, request.Kind, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var created = PolicyDocument.Create(request.TenantId, request.EventId, request.Kind, bodyHtml, now);
            repository.Add(created);
            await repository.SaveChangesAsync(cancellationToken);
            return new SetPolicyDocumentResult(SetPolicyDocumentOutcome.Saved, created.Version);
        }

        existing.Revise(bodyHtml, now);
        await repository.SaveChangesAsync(cancellationToken);
        return new SetPolicyDocumentResult(SetPolicyDocumentOutcome.Saved, existing.Version);
    }
}
