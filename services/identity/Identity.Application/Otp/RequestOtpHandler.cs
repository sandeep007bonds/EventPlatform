namespace Identity.Application.Otp;

/// <summary>
/// Issues a new OTP challenge, subject to a per-phone-number resend cooldown, and hands the
/// code to <see cref="IOtpSender"/>. The challenge is persisted BEFORE the send is attempted, so
/// the cooldown is recorded even if the send later fails (prevents an attacker from bypassing the
/// cooldown by forcing sends to fail).
/// </summary>
/// <param name="repository">The identity repository.</param>
/// <param name="hasher">The OTP hasher.</param>
/// <param name="sender">The OTP sender.</param>
public sealed class RequestOtpHandler(IIdentityRepository repository, IOtpHasher hasher, IOtpSender sender)
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    /// <summary>Handles a <see cref="RequestOtpCommand"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the request.</returns>
    public async Task<RequestOtpResult> HandleAsync(RequestOtpCommand command, CancellationToken cancellationToken)
    {
        if (!RequestOtpValidator.IsValid(command.PhoneNumber))
        {
            return new RequestOtpResult(RequestOtpOutcome.InvalidPhoneNumber, null, null);
        }

        var now = DateTimeOffset.UtcNow;
        var latest = await repository.GetLatestPhoneVerificationAsync(command.PhoneNumber, cancellationToken);
        if (latest is not null && now - latest.CreatedAt < ResendCooldown)
        {
            var retryAfter = (int)Math.Ceiling((ResendCooldown - (now - latest.CreatedAt)).TotalSeconds);
            return new RequestOtpResult(RequestOtpOutcome.RateLimited, retryAfter, null);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var salt = hasher.GenerateSalt();
        var hash = hasher.Hash(code, salt);

        var verification = PhoneVerification.Issue(command.PhoneNumber, hash, salt, Ttl);
        repository.AddPhoneVerification(verification);
        await repository.SaveChangesAsync(cancellationToken);

        var sent = await sender.SendAsync(command.PhoneNumber, code, (int)Ttl.TotalMinutes, cancellationToken);
        return sent
            ? new RequestOtpResult(RequestOtpOutcome.Sent, null, (int)Ttl.TotalSeconds)
            : new RequestOtpResult(RequestOtpOutcome.SendFailed, null, null);
    }
}
