namespace EventPlatform.Gateway.DevAuth;

/// <summary>The resolved dev user returned alongside the issued token.</summary>
/// <param name="Sub">The user id (deterministically derived from the email).</param>
/// <param name="Email">The email used to log in.</param>
/// <param name="TenantId">The tenant the token was issued for.</param>
/// <param name="Role">The optional role claim, if requested.</param>
public sealed record DevLoginUser(Guid Sub, string Email, Guid TenantId, string? Role);
