namespace Identity.Tests.Organizers;

public sealed class LoginOrganizerHandlerTests
{
    private readonly IOrganizerRepository repository = Substitute.For<IOrganizerRepository>();
    private readonly IPasswordHasher hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly LoginOrganizerHandler handler;

    public LoginOrganizerHandlerTests()
    {
        handler = new LoginOrganizerHandler(repository, hasher, tokenIssuer);
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_ReturnsInvalidCredentials()
    {
        repository.GetOrganizerByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>())
            .Returns((OrganizerAccount?)null);

        var result = await handler.HandleAsync(new LoginOrganizerCommand("nobody@example.com", "password123"), CancellationToken.None);

        result.Outcome.ShouldBe(LoginOrganizerOutcome.InvalidCredentials);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_IncrementsFailedCount_NotLockedBeforeFifth()
    {
        var account = OrganizerAccount.Register(Guid.NewGuid(), "organizer@example.com", "hash");
        repository.GetOrganizerByEmailAsync("organizer@example.com", Arg.Any<CancellationToken>()).Returns(account);
        hasher.Verify(account, Arg.Any<string>()).Returns(false);

        var result = await handler.HandleAsync(new LoginOrganizerCommand("organizer@example.com", "wrong"), CancellationToken.None);

        result.Outcome.ShouldBe(LoginOrganizerOutcome.InvalidCredentials);
        account.FailedLoginCount.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_FifthWrongPassword_ReturnsLockedOut()
    {
        var account = OrganizerAccount.Register(Guid.NewGuid(), "organizer@example.com", "hash");
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < OrganizerAccount.MaxFailedAttempts - 1; i++)
        {
            account.RecordFailedLogin(now);
        }

        repository.GetOrganizerByEmailAsync("organizer@example.com", Arg.Any<CancellationToken>()).Returns(account);
        hasher.Verify(account, Arg.Any<string>()).Returns(false);

        var result = await handler.HandleAsync(new LoginOrganizerCommand("organizer@example.com", "wrong"), CancellationToken.None);

        result.Outcome.ShouldBe(LoginOrganizerOutcome.LockedOut);
    }

    [Fact]
    public async Task HandleAsync_LockedOutAccount_ReturnsLockedOut_WithoutCheckingPassword()
    {
        var account = OrganizerAccount.Register(Guid.NewGuid(), "organizer@example.com", "hash");
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < OrganizerAccount.MaxFailedAttempts; i++)
        {
            account.RecordFailedLogin(now);
        }

        repository.GetOrganizerByEmailAsync("organizer@example.com", Arg.Any<CancellationToken>()).Returns(account);

        var result = await handler.HandleAsync(new LoginOrganizerCommand("organizer@example.com", "password123"), CancellationToken.None);

        result.Outcome.ShouldBe(LoginOrganizerOutcome.LockedOut);
        hasher.DidNotReceive().Verify(Arg.Any<OrganizerAccount>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_CorrectPassword_IssuesAnOrganizerToken_WithTheAccountsTenant()
    {
        var tenantId = Guid.NewGuid();
        var account = OrganizerAccount.Register(tenantId, "organizer@example.com", "hash");
        repository.GetOrganizerByEmailAsync("organizer@example.com", Arg.Any<CancellationToken>()).Returns(account);
        hasher.Verify(account, "password123").Returns(true);

        var issuedToken = new IssuedAccessToken("token", DateTimeOffset.UtcNow.AddDays(7));
        tokenIssuer.IssueAsync(account.Id, "organizer", tenantId, Arg.Any<CancellationToken>()).Returns(issuedToken);

        var result = await handler.HandleAsync(new LoginOrganizerCommand("organizer@example.com", "password123"), CancellationToken.None);

        result.Outcome.ShouldBe(LoginOrganizerOutcome.LoggedIn);
        result.Token.ShouldBe(issuedToken);
        result.OrganizerId.ShouldBe(account.Id);
        result.TenantId.ShouldBe(tenantId);
        account.FailedLoginCount.ShouldBe(0);
    }
}
