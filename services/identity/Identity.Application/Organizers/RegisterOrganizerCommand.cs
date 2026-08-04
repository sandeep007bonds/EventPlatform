namespace Identity.Application.Organizers;

/// <summary>A request to register a new organization and its first organizer account.</summary>
/// <param name="OrganizationName">The organization's display name.</param>
/// <param name="Email">The organizer's login email.</param>
/// <param name="Password">The organizer's plaintext password.</param>
public sealed record RegisterOrganizerCommand(string OrganizationName, string Email, string Password);
