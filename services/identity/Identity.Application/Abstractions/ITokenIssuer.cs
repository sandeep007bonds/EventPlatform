namespace Identity.Application.Abstractions;

/// <summary>Mints and cryptographically signs a buyer access token. Implemented in Infrastructure.</summary>
public interface ITokenIssuer
{
    /// <summary>Issues a new access token for the given buyer.</summary>
    /// <param name="buyerId">The buyer's stable id (becomes the <c>sub</c> claim).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IssuedAccessToken> IssueAsync(Guid buyerId, CancellationToken cancellationToken);
}
