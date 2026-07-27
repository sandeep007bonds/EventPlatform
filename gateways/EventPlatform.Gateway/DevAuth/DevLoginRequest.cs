namespace EventPlatform.Gateway.DevAuth;

/// <summary>
/// Development-only login request. The "password" a real login form would show is decorative here
/// — dev-login authenticates by email alone, never checked against anything. See
/// <see cref="DevAuthEndpoints"/> for the gating that keeps this endpoint out of Production entirely.
/// </summary>
/// <param name="Email">The email identifying the dev user (maps deterministically to a stable id).</param>
/// <param name="TenantId">The tenant to issue the token for; defaults to the shared dev tenant if omitted.</param>
/// <param name="Role">
/// Optional role (<c>buyer</c> or <c>organizer</c>) so the frontend can route to the right themed
/// section post-login. Not enforced by any backend authorization today.
/// </param>
public sealed record DevLoginRequest(string Email, Guid? TenantId, string? Role);
