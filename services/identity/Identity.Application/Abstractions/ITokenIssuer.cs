namespace Identity.Application.Abstractions;

/// <summary>
/// Mints and cryptographically signs an access token, for either a buyer or an organizer
/// subject. Implemented in Infrastructure.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>Issues a new access token.</summary>
    /// <param name="subjectId">The subject's stable id (becomes the <c>sub</c> claim).</param>
    /// <param name="role">The <c>role</c> claim value — <c>"buyer"</c> or <c>"organizer"</c>.</param>
    /// <param name="tenantId">
    /// The <c>tenant_id</c> claim, for a tenant-scoped organizer token. <see langword="null"/> for
    /// a buyer token, which is deliberately tenant-less (ADR-0022).
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IssuedAccessToken> IssueAsync(Guid subjectId, string role, Guid? tenantId, CancellationToken cancellationToken);
}
