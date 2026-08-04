namespace Identity.Infrastructure.Signing;

/// <summary>
/// Persistence for <see cref="SigningKey"/> rows. Kept separate from <c>IIdentityRepository</c>
/// because only <c>Identity.Infrastructure</c> ever needs "the signing key" — nothing in
/// <c>Identity.Application</c> does.
/// </summary>
internal interface ISigningKeyRepository
{
    /// <summary>Returns the current active key, if one has been generated yet.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The active key, or <see langword="null"/>.</returns>
    Task<SigningKey?> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>Registers a new key to be persisted.</summary>
    /// <param name="key">The key to add.</param>
    void Add(SigningKey key);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
