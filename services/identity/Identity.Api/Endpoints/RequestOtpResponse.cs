namespace Identity.Api.Endpoints;

/// <summary>Response body for a successful <c>POST /v1/otp/request</c>.</summary>
/// <param name="ExpiresInSeconds">How long the code stays valid.</param>
public sealed record RequestOtpResponse(int ExpiresInSeconds);
