namespace Identity.Api.Endpoints;

/// <summary>Request body for <c>POST /v1/organizers/login</c>.</summary>
/// <param name="Email">The login email.</param>
/// <param name="Password">The plaintext password.</param>
public sealed record LoginOrganizerRequest(string Email, string Password);
