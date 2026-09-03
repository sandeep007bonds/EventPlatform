namespace Catalog.Application.Features.AddEventSession;

/// <summary>
/// Handles <see cref="AddEventSessionCommand"/>.
/// </summary>
/// <remarks>
/// Allowed on a published event: adding a late show to a run that is already selling is ordinary
/// work. The new performance starts as a draft, so nothing about the event's existing sales changes
/// when it appears — it goes live only when it has a seat map and its own publish.
/// </remarks>
/// <param name="repository">The event repository.</param>
internal sealed class AddEventSessionHandler(IEventRepository repository)
    : IRequestHandler<AddEventSessionCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(AddEventSessionCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return SessionCommandResult.NotFound();
        }

        // The aggregate owns the overlap and date rules, because it is the only thing that can see
        // every performance at once. Catching them here turns an invariant into a 409 rather than
        // a 500, without the handler re-implementing a check that could then disagree.
        try
        {
            var session = @event.AddSession(
                request.Name,
                request.StartsAt,
                request.EndsAt,
                request.DoorsOpenAt,
                request.BookingEndsAt);

            await repository.SaveChangesAsync(cancellationToken);

            return SessionCommandResult.Ok(session.ToResponse());
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return SessionCommandResult.Refused(exception.Message);
        }
    }
}
