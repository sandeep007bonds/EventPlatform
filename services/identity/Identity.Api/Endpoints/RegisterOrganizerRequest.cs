namespace Identity.Api.Endpoints;

/// <summary>Request body for <c>POST /v1/organizers/register</c>.</summary>
/// <param name="OrganizationName">The new organization's display name.</param>
/// <param name="Email">The first organizer's login email.</param>
/// <param name="Password">The first organizer's plaintext password.</param>
public sealed record RegisterOrganizerRequest(string OrganizationName, string Email, string Password);
