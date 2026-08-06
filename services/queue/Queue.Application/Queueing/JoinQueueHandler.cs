namespace Queue.Application.Queueing;

/// <summary>
/// Joins (or resumes) a session's place in an event's waiting room. When the event has no
/// queueing enabled — either never provisioned, or provisioned with <c>Enabled = false</c> — this
/// is a one-branch no-op admit, with no call to <see cref="IQueueStore"/> at all.
/// </summary>
/// <param name="settingsRepository">The queue-settings repository.</param>
/// <param name="store">The waiting-room store.</param>
/// <param name="tokenIssuer">The admission-token issuer.</param>
public sealed class JoinQueueHandler(IQueueSettingsRepository settingsRepository, IQueueStore store, IAdmissionTokenIssuer tokenIssuer)
{
    /// <summary>Handles a join request.</summary>
    /// <param name="eventId">The event to join the queue for.</param>
    /// <param name="sessionId">The client-generated session id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The resulting status.</returns>
    public async Task<QueueStatusResponse> HandleAsync(Guid eventId, Guid sessionId, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetByIdAsync(eventId, cancellationToken);
        if (settings is null || !settings.Enabled)
        {
            return QueueStatusResponseFactory.ImmediateAdmit(tokenIssuer, eventId, sessionId);
        }

        var result = await store.EnqueueOrResumeAsync(eventId, sessionId, cancellationToken);
        return QueueStatusResponseFactory.FromStoreResult(settings, result, tokenIssuer, eventId, sessionId);
    }
}
