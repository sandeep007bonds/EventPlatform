namespace Catalog.Application.Features.CancelEventSession;

/// <summary>
/// Handles <see cref="CancelEventSessionCommand"/>.
/// </summary>
/// <remarks>
/// Cancels the performance and announces it, but does <b>not</b> refund anything. Working out who
/// bought what and giving their money back is a saga with approval and compensation in it, not a
/// side effect of a status change — see the change-policy work. Downstream services stop selling
/// against this performance on the event below.
/// </remarks>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class CancelEventSessionHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<CancelEventSessionCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(
        CancelEventSessionCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return SessionCommandResult.NotFound();
        }

        var session = @event.FindSession(request.EventSessionId);
        if (session is null)
        {
            return SessionCommandResult.NotFound();
        }

        var wasPublished = session.Status == EventSessionStatus.Published;

        try
        {
            session.Cancel();
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }

        // Only a performance that was actually selling is worth announcing. Cancelling a draft
        // nobody could buy is bookkeeping, and a consumer that acted on it would be reacting to a
        // performance it never had inventory for.
        if (wasPublished)
        {
            events.Enqueue(new EventSessionCancelled(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                @event.TenantId,
                @event.Id,
                session.Id));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return SessionCommandResult.Ok(session.ToResponse());
    }
}
