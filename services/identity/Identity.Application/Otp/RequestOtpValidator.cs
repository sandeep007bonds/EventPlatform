namespace Identity.Application.Otp;

/// <summary>Hand-written validation for <see cref="RequestOtpCommand"/> — no FluentValidation, matching Communication's lean style.</summary>
public static class RequestOtpValidator
{
    // ^\+[1-9]\d{1,14}$ — E.164: leading +, no leading 0, 2-15 digits total.
    private static readonly Regex E164 = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);

    /// <summary>Checks whether a phone number is a well-formed E.164 value.</summary>
    /// <param name="phoneNumber">The phone number to validate.</param>
    /// <returns><see langword="true"/> if valid.</returns>
    public static bool IsValid(string phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && E164.IsMatch(phoneNumber);
}
