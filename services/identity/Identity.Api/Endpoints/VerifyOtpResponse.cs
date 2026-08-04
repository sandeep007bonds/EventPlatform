namespace Identity.Api.Endpoints;

/// <summary>Response body for a successful <c>POST /v1/otp/verify</c>.</summary>
/// <param name="AccessToken">The issued JWT.</param>
/// <param name="TokenType">Always <c>"Bearer"</c>.</param>
/// <param name="ExpiresAt">When the token stops being valid (UTC).</param>
/// <param name="BuyerId">The buyer's stable id (matches the token's <c>sub</c> claim).</param>
public sealed record VerifyOtpResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, Guid BuyerId);
