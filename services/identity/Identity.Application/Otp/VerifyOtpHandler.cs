namespace Identity.Application.Otp;

/// <summary>
/// Checks a submitted code against the latest challenge for a phone number, then — on success —
/// resolves/creates the buyer account and issues a token. The challenge update and the buyer
/// upsert are saved together in one <see cref="IIdentityRepository.SaveChangesAsync"/> call.
/// </summary>
/// <param name="repository">The identity repository.</param>
/// <param name="hasher">The OTP hasher.</param>
/// <param name="tokenIssuer">The token issuer.</param>
public sealed class VerifyOtpHandler(IIdentityRepository repository, IOtpHasher hasher, ITokenIssuer tokenIssuer)
{
    /// <summary>Handles a <see cref="VerifyOtpCommand"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<VerifyOtpResult> HandleAsync(VerifyOtpCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var verification = await repository.GetLatestPhoneVerificationAsync(command.PhoneNumber, cancellationToken);

        if (verification is null || verification.Status != PhoneVerificationStatus.Pending)
        {
            return new VerifyOtpResult(VerifyOtpOutcome.NoActiveChallenge, null, null);
        }

        if (verification.HasExpired(now))
        {
            verification.MarkExpired(now);
            await repository.SaveChangesAsync(cancellationToken);
            return new VerifyOtpResult(VerifyOtpOutcome.Expired, null, null);
        }

        if (!hasher.Verify(command.Code, verification.Salt, verification.OtpHash))
        {
            var lockedOut = verification.RecordFailedAttempt();
            await repository.SaveChangesAsync(cancellationToken);
            return new VerifyOtpResult(lockedOut ? VerifyOtpOutcome.LockedOut : VerifyOtpOutcome.WrongCode, null, null);
        }

        verification.MarkVerified(now);

        var buyer = await repository.GetBuyerAccountByPhoneNumberAsync(command.PhoneNumber, cancellationToken);
        if (buyer is null)
        {
            buyer = BuyerAccount.CreateFromVerification(command.PhoneNumber);
            repository.AddBuyerAccount(buyer);
        }
        else
        {
            buyer.RecordVerification(now);
        }

        await repository.SaveChangesAsync(cancellationToken);

        var token = await tokenIssuer.IssueAsync(buyer.Id, cancellationToken);
        return new VerifyOtpResult(VerifyOtpOutcome.Verified, token, buyer.Id);
    }
}
