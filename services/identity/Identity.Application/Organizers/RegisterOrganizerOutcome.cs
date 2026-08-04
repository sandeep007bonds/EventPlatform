namespace Identity.Application.Organizers;

/// <summary>Outcome of a <see cref="RegisterOrganizerCommand"/>.</summary>
public enum RegisterOrganizerOutcome
{
    /// <summary>The tenant and organizer account were created and a token was issued.</summary>
    Registered,

    /// <summary>The submitted organization name, email, or password failed validation.</summary>
    ValidationFailed,

    /// <summary>An organizer account already exists for this email.</summary>
    EmailAlreadyRegistered,
}
