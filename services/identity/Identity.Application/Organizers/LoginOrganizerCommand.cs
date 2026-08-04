namespace Identity.Application.Organizers;

/// <summary>A request to log in with an existing organizer email+password.</summary>
/// <param name="Email">The login email.</param>
/// <param name="Password">The plaintext password.</param>
public sealed record LoginOrganizerCommand(string Email, string Password);
