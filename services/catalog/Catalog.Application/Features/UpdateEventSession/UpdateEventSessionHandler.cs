namespace Catalog.Application.Features.UpdateEventSession;

/// <summary>Handles <see cref="UpdateEventSessionCommand"/>.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class UpdateEventSessionHandler(IEventRepository repository)
    : IRequestHandler<UpdateEventSessionCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(UpdateEventSessionCommand request, CancellationToken cancellationToken)
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
            // Rescheduling goes through the event, not the session: only the event can see whether
            // the new times collide with another performance, and it also owns the cached date
            // range the storefront lists by.
            @event.RescheduleSession(
                request.EventSessionId,
                request.StartsAt,
                request.EndsAt,
                request.DoorsOpenAt,
                request.BookingEndsAt);

            session.Rename(request.Name);

            await repository.SaveChangesAsync(cancellationToken);

            return SessionCommandResult.Ok(session.ToResponse());
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return SessionCommandResult.Refused(exception.Message);
        }
    }
}
