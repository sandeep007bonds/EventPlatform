namespace Identity.Application.Otp;

/// <summary>The result of a <see cref="VerifyOtpCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="Token">Populated only for <see cref="VerifyOtpOutcome.Verified"/>.</param>
/// <param name="BuyerId">Populated only for <see cref="VerifyOtpOutcome.Verified"/>.</param>
public sealed record VerifyOtpResult(VerifyOtpOutcome Outcome, IssuedAccessToken? Token, Guid? BuyerId);
