namespace Queue.Application.Queueing;

/// <summary>Shared response-shaping logic used by both <see cref="JoinQueueHandler"/> and <see cref="QueueStatusHandler"/>.</summary>
internal static class QueueStatusResponseFactory
{
    // Used only when an event has no provisioned settings yet (or queueing isn't enabled for it) —
    // sensible enough that a buyer isn't stranded with an unusably short admission window.
    private static readonly TimeSpan DefaultAdmissionTtl = TimeSpan.FromMinutes(10);

    /// <summary>Mints an immediate admission — the "queueing is a no-op for this event" path.</summary>
    /// <param name="tokenIssuer">The admission-token issuer.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="sessionId">The session id.</param>
    /// <returns>An already-admitted response.</returns>
    public static QueueStatusResponse ImmediateAdmit(IAdmissionTokenIssuer tokenIssuer, Guid eventId, Guid sessionId) =>
        new(true, tokenIssuer.Issue(eventId, sessionId, DefaultAdmissionTtl), null, null);

    /// <summary>Maps a store result to the API response shape, minting a token if the session is admitted.</summary>
    /// <param name="settings">The event's provisioned, enabled settings.</param>
    /// <param name="result">The store's reported status.</param>
    /// <param name="tokenIssuer">The admission-token issuer.</param>
    /// <param name="eventId">The event id.</param>
    /// <param name="sessionId">The session id.</param>
    /// <returns>The mapped response.</returns>
    public static QueueStatusResponse FromStoreResult(
        QueueSettings settings,
        QueueStoreResult result,
        IAdmissionTokenIssuer tokenIssuer,
        Guid eventId,
        Guid sessionId)
    {
        if (result.Status == QueueSessionStatus.Admitted)
        {
            var ttl = TimeSpan.FromSeconds(settings.SessionTtlSeconds);
            return new QueueStatusResponse(true, tokenIssuer.Issue(eventId, sessionId, ttl), null, null);
        }

        var estimatedWaitSeconds = result.Position is { } position
            ? position / settings.AdmissionRatePerInterval * settings.IntervalSeconds
            : (int?)null;

        return new QueueStatusResponse(false, null, result.Position, estimatedWaitSeconds, result.WasCreated);
    }
}
