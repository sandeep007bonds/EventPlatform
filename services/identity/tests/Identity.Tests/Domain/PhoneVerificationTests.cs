namespace Identity.Tests.Domain;

public sealed class PhoneVerificationTests
{
    [Fact]
    public void RecordFailedAttempt_LocksOutOnlyOnTheFifthAttempt()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));

        for (var i = 0; i < PhoneVerification.MaxAttempts - 1; i++)
        {
            verification.RecordFailedAttempt().ShouldBeFalse();
        }

        verification.Status.ShouldBe(PhoneVerificationStatus.Pending);
        verification.RecordFailedAttempt().ShouldBeTrue();
        verification.Status.ShouldBe(PhoneVerificationStatus.Failed);
    }

    [Fact]
    public void RecordFailedAttempt_OnNonPendingChallenge_Throws()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        verification.MarkVerified(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => verification.RecordFailedAttempt());
    }

    [Fact]
    public void MarkVerified_OnNonPendingChallenge_Throws()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        verification.MarkVerified(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => verification.MarkVerified(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void HasExpired_IsTrueExactlyAtExpiresAt()
    {
        var now = DateTimeOffset.UtcNow;
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));

        verification.HasExpired(verification.ExpiresAt).ShouldBeTrue();
        verification.HasExpired(verification.ExpiresAt.AddSeconds(-1)).ShouldBeFalse();
    }

    [Fact]
    public void MarkExpired_OnNonExpiredChallenge_Throws()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));

        Should.Throw<InvalidOperationException>(() => verification.MarkExpired(DateTimeOffset.UtcNow));
    }
}
