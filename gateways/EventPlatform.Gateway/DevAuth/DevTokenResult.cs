namespace EventPlatform.Gateway.DevAuth;

/// <summary>Result of issuing a dev token.</summary>
/// <param name="AccessToken">The signed JWT.</param>
/// <param name="ExpiresAt">When the token expires (UTC).</param>
/// <param name="User">The resolved dev user.</param>
public sealed record DevTokenResult(string AccessToken, DateTimeOffset ExpiresAt, DevLoginUser User);
