namespace Catalog.Application.Features.AttachSessionSeatMap;

/// <summary>
/// Handles <see cref="AttachSessionSeatMapCommand"/> by resolving the seat-map version from the
/// Venue service and pinning it to the performance.
/// </summary>
/// <remarks>
/// Three things are checked before the map is accepted, and each of them is a real failure someone
/// would otherwise hit at publish time or later: the map exists, it belongs to the same tenant, and
/// the version is <b>published</b> — a draft version is still being edited, so pinning one would
/// mean selling seats that can still move.
/// </remarks>
/// <param name="repository">The event repository.</param>
/// <param name="venue">The Venue service client.</param>
internal sealed class AttachSessionSeatMapHandler(IEventRepository repository, IVenueClient venue)
    : IRequestHandler<AttachSessionSeatMapCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(
        AttachSessionSeatMapCommand request,
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

        var version = await venue.GetSeatMapVersionAsync(request.SeatMapId, request.VersionNumber, cancellationToken);
        if (version is null || version.TenantId != request.TenantId)
        {
            return SessionCommandResult.Refused(
                "That seat map does not exist, or it belongs to another organizer.");
        }

        if (!version.IsPublished)
        {
            return SessionCommandResult.Refused(
                "That seat-map version is still a draft. Publish it in the venue before selling against it.");
        }

        try
        {
            session.AttachSeatMap(
                version.VenueId,
                version.SeatMapId,
                version.SeatMapVersionId,
                version.VersionNumber,
                new VenueSnapshot(version.VenueName, version.City, version.Country, version.TimeZoneId));

            await repository.SaveChangesAsync(cancellationToken);

            return SessionCommandResult.Ok(session.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }
    }
}
