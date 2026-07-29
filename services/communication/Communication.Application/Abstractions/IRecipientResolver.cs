namespace Communication.Application.Abstractions;

/// <summary>
/// Resolves a buyer's contact info from their user id. No Identity/user-profile service exists yet
/// to back this, so the only implementation today always returns <see langword="null"/> — see
/// <c>services/communication/CLAUDE.md</c> for the deferred-delivery decision this supports.
/// </summary>
public interface IRecipientResolver
{
    /// <summary>Attempts to resolve a user's contact info.</summary>
    /// <param name="userId">The user id, from an integration event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The resolved contact info, or <see langword="null"/> if it cannot be resolved.</returns>
    Task<RecipientContact?> ResolveAsync(Guid userId, CancellationToken cancellationToken);
}
