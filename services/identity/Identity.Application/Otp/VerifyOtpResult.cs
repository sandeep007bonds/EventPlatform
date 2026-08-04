namespace Identity.Application.Otp;

/// <summary>The result of a <see cref="VerifyOtpCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="Token">The issued access token, populated only on <see cref="VerifyOtpOutcome.Verified"/>.</param>
/// <param name="BuyerId">The buyer's stable id, populated only on <see cref="VerifyOtpOutcome.Verified"/>.</param>
public sealed record VerifyOtpResult(VerifyOtpOutcome Outcome, IssuedAccessToken? Token, Guid? BuyerId);
