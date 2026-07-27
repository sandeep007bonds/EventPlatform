namespace EventPlatform.Gateway.DevAuth;

/// <summary>Response body for a successful dev login.</summary>
/// <param name="AccessToken">The bearer token to send as <c>Authorization: Bearer &lt;token&gt;</c>.</param>
/// <param name="ExpiresIn">Seconds until the token expires.</param>
/// <param name="TokenType">Always <c>Bearer</c>.</param>
/// <param name="User">The resolved dev user.</param>
public sealed record DevLoginResponse(string AccessToken, int ExpiresIn, string TokenType, DevLoginUser User);
