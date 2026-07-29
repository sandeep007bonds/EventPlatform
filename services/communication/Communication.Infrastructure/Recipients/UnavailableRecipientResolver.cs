namespace Communication.Infrastructure.Recipients;

/// <summary>
/// The only <see cref="IRecipientResolver"/> implementation today. No Identity/user-profile service
/// exists yet to resolve a buyer's <c>UserId</c> to contact info, so this always returns
/// <see langword="null"/> — subscribers record a <c>Skipped</c> delivery-log row instead of failing
/// or silently dropping the event (see <see cref="IntegrationEventNotificationHandler"/>). Replace
/// this adapter (not the port, not its callers) once a recipient-lookup source exists.
/// </summary>
internal sealed class UnavailableRecipientResolver : IRecipientResolver
{
    /// <inheritdoc />
    public Task<RecipientContact?> ResolveAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<RecipientContact?>(null);
}
