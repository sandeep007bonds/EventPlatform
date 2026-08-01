namespace Identity.Api.Endpoints;

/// <summary>Request body for <c>POST /v1/otp/verify</c>.</summary>
/// <param name="PhoneNumber">The E.164 phone number the code was sent to.</param>
/// <param name="Code">The submitted 6-digit code.</param>
public sealed record VerifyOtpRequest(string PhoneNumber, string Code);
