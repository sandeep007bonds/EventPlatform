namespace Queue.Application.Queueing;

/// <summary>Reads a session's current waiting-room status — used for polling. Never enqueues.</summary>
/// <param name="settingsRepository">The queue-settings repository.</param>
/// <param name="store">The waiting-room store.</param>
/// <param name="tokenIssuer">The admission-token issuer.</param>
public sealed class QueueStatusHandler(IQueueSettingsRepository settingsRepository, IQueueStore store, IAdmissionTokenIssuer tokenIssuer)
{
    /// <summary>Handles a status poll.</summary>
    /// <param name="eventId">The event being queued for.</param>
    /// <param name="sessionId">The session id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The session's current status.</returns>
    public async Task<QueueStatusResponse> HandleAsync(Guid eventId, Guid sessionId, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetByIdAsync(eventId, cancellationToken);
        if (settings is null || !settings.Enabled)
        {
            return QueueStatusResponseFactory.ImmediateAdmit(tokenIssuer, eventId, sessionId);
        }

        var result = await store.GetStatusAsync(eventId, sessionId, cancellationToken);
        return QueueStatusResponseFactory.FromStoreResult(settings, result, tokenIssuer, eventId, sessionId);
    }
}
