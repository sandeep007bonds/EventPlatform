namespace Catalog.Application.Features.RemoveEventSession;

/// <summary>Handles <see cref="RemoveEventSessionCommand"/>.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class RemoveEventSessionHandler(IEventRepository repository)
    : IRequestHandler<RemoveEventSessionCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(RemoveEventSessionCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return SessionCommandResult.NotFound();
        }

        if (@event.FindSession(request.EventSessionId) is null)
        {
            return SessionCommandResult.NotFound();
        }

        try
        {
            @event.RemoveSession(request.EventSessionId);
            await repository.SaveChangesAsync(cancellationToken);

            return SessionCommandResult.Removed();
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }
    }
}
