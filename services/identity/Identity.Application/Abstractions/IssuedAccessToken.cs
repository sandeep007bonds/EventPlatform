namespace Identity.Application.Abstractions;

/// <summary>An issued, signed access token.</summary>
/// <param name="AccessToken">The compact JWS.</param>
/// <param name="ExpiresAt">When the token stops being valid (UTC).</param>
public sealed record IssuedAccessToken(string AccessToken, DateTimeOffset ExpiresAt);
