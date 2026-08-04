namespace Identity.Tests.Domain;

public sealed class OrganizerAccountTests
{
    [Fact]
    public void RecordFailedLogin_LocksOutOnlyOnTheFifthAttempt()
    {
        var account = OrganizerAccount.Register(Guid.NewGuid(), "organizer@example.com", "hash");
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < OrganizerAccount.MaxFailedAttempts - 1; i++)
        {
            account.RecordFailedLogin(now).ShouldBeFalse();
        }

        account.IsLockedOut(now).ShouldBeFalse();
        account.RecordFailedLogin(now).ShouldBeTrue();
        account.IsLockedOut(now).ShouldBeTrue();
        account.IsLockedOut(now.Add(OrganizerAccount.LockoutDuration)).ShouldBeFalse();
    }

    [Fact]
    public void RecordSuccessfulLogin_ClearsFailedAttemptsAndLockout()
    {
        var account = OrganizerAccount.Register(Guid.NewGuid(), "organizer@example.com", "hash");
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < OrganizerAccount.MaxFailedAttempts; i++)
        {
            account.RecordFailedLogin(now);
        }

        account.IsLockedOut(now).ShouldBeTrue();

        account.RecordSuccessfulLogin(now);

        account.IsLockedOut(now).ShouldBeFalse();
        account.FailedLoginCount.ShouldBe(0);
        account.LastLoginAt.ShouldBe(now);
    }
}
