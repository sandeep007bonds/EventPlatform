namespace Identity.Api.Endpoints;

/// <summary>Request body for <c>POST /v1/otp/request</c>.</summary>
/// <param name="PhoneNumber">The E.164 phone number to send a code to.</param>
public sealed record RequestOtpRequest(string PhoneNumber);
