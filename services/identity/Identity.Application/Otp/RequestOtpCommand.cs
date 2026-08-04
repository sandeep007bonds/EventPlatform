namespace Identity.Application.Otp;

/// <summary>A request to send an OTP code to a phone number.</summary>
/// <param name="PhoneNumber">The E.164 phone number.</param>
public sealed record RequestOtpCommand(string PhoneNumber);
