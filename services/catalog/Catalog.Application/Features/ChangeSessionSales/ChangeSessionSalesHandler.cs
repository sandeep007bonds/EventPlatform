namespace Catalog.Application.Features.ChangeSessionSales;

/// <summary>
/// Handles <see cref="ChangeSessionSalesCommand"/> by pausing or resuming one performance and
/// enqueuing the matching integration event in the same unit of work.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class ChangeSessionSalesHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<ChangeSessionSalesCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(
        ChangeSessionSalesCommand request,
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

        try
        {
            if (request.Pause)
            {
                session.PauseSales();
            }
            else
            {
                session.ResumeSales();
            }
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }

        events.Enqueue(SessionSalesEvent.For(request.Pause, @event, session));

        await repository.SaveChangesAsync(cancellationToken);

        return SessionCommandResult.Ok(session.ToResponse());
    }
}
