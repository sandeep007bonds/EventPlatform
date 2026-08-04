namespace Identity.Application.Organizers;

/// <summary>Outcome of a <see cref="LoginOrganizerCommand"/>.</summary>
public enum LoginOrganizerOutcome
{
    /// <summary>The credentials matched; an access token was issued.</summary>
    LoggedIn,

    /// <summary>
    /// No account exists for this email, or the password did not match. Deliberately not split
    /// into separate outcomes — distinguishing them would let a caller enumerate registered emails.
    /// </summary>
    InvalidCredentials,

    /// <summary>The account is locked out after too many failed attempts.</summary>
    LockedOut,
}
