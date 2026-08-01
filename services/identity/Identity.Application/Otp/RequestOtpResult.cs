namespace Identity.Application.Otp;

/// <summary>The result of a <see cref="RequestOtpCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="RetryAfterSeconds">Populated only for <see cref="RequestOtpOutcome.RateLimited"/>.</param>
/// <param name="ExpiresInSeconds">Populated only for <see cref="RequestOtpOutcome.Sent"/>.</param>
public sealed record RequestOtpResult(RequestOtpOutcome Outcome, int? RetryAfterSeconds, int? ExpiresInSeconds);
