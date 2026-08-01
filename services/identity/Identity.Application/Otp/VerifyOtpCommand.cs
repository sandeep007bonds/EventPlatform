namespace Identity.Application.Otp;

/// <summary>A request to verify a submitted OTP code.</summary>
/// <param name="PhoneNumber">The E.164 phone number the code was sent to.</param>
/// <param name="Code">The submitted 6-digit code.</param>
public sealed record VerifyOtpCommand(string PhoneNumber, string Code);
