namespace Identity.Application.Organizers;

/// <summary>Hand-written validation for <see cref="RegisterOrganizerCommand"/> — no FluentValidation, matching the OTP slices' lean style.</summary>
public static class RegisterOrganizerValidator
{
    private const int MaxOrganizationNameLength = 200;
    private const int MaxEmailLength = 320;
    private const int MinPasswordLength = 8;

    /// <summary>Checks whether a <see cref="RegisterOrganizerCommand"/> is well-formed.</summary>
    /// <param name="command">The command to validate.</param>
    /// <returns><see langword="true"/> if every field passes validation.</returns>
    public static bool IsValid(RegisterOrganizerCommand command) =>
        !string.IsNullOrWhiteSpace(command.OrganizationName)
        && command.OrganizationName.Length <= MaxOrganizationNameLength
        && !string.IsNullOrWhiteSpace(command.Email)
        && command.Email.Length <= MaxEmailLength
        && MailAddress.TryCreate(command.Email, out _)
        && !string.IsNullOrWhiteSpace(command.Password)
        && command.Password.Length >= MinPasswordLength;
}
