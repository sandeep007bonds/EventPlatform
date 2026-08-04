namespace Identity.Tests.Otp;

public sealed class VerifyOtpHandlerTests
{
    private readonly IIdentityRepository repository = Substitute.For<IIdentityRepository>();
    private readonly IOtpHasher hasher = Substitute.For<IOtpHasher>();
    private readonly ITokenIssuer tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly VerifyOtpHandler handler;

    public VerifyOtpHandlerTests()
    {
        handler = new VerifyOtpHandler(repository, hasher, tokenIssuer);
    }

    [Fact]
    public async Task HandleAsync_NoChallenge_ReturnsNoActiveChallenge()
    {
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns((PhoneVerification?)null);

        var result = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "123456"), CancellationToken.None);

        result.Outcome.ShouldBe(VerifyOtpOutcome.NoActiveChallenge);
    }

    [Fact]
    public async Task HandleAsync_ExpiredChallenge_ReturnsExpired_WithoutComparingHash()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(-1));
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(verification);

        var result = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "123456"), CancellationToken.None);

        result.Outcome.ShouldBe(VerifyOtpOutcome.Expired);
        hasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_WrongCode_IncrementsAttempts_NotLockedBeforeFifth()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(verification);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "000000"), CancellationToken.None);

        result.Outcome.ShouldBe(VerifyOtpOutcome.WrongCode);
        verification.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_FifthWrongAttempt_ReturnsLockedOut()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        for (var i = 0; i < PhoneVerification.MaxAttempts - 1; i++)
        {
            verification.RecordFailedAttempt();
        }

        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(verification);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "000000"), CancellationToken.None);

        result.Outcome.ShouldBe(VerifyOtpOutcome.LockedOut);
    }

    [Fact]
    public async Task HandleAsync_CorrectCode_FirstTime_CreatesBuyerAccount_AndIssuesToken()
    {
        var verification = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(verification);
        repository.GetBuyerAccountByPhoneNumberAsync("+15550000000", Arg.Any<CancellationToken>()).Returns((BuyerAccount?)null);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var issuedToken = new IssuedAccessToken("token", DateTimeOffset.UtcNow.AddDays(7));
        tokenIssuer.IssueAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(issuedToken);

        var result = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "123456"), CancellationToken.None);

        result.Outcome.ShouldBe(VerifyOtpOutcome.Verified);
        result.Token.ShouldBe(issuedToken);
        result.BuyerId.ShouldNotBeNull();
        repository.Received(1).AddBuyerAccount(Arg.Any<BuyerAccount>());
    }

    [Fact]
    public async Task HandleAsync_CorrectCode_SecondIndependentCycle_ReusesSameBuyerId()
    {
        // First OTP cycle: no existing account, one gets created.
        var firstChallenge = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(firstChallenge);
        repository.GetBuyerAccountByPhoneNumberAsync("+15550000000", Arg.Any<CancellationToken>()).Returns((BuyerAccount?)null);
        hasher.Verify(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        tokenIssuer.IssueAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new IssuedAccessToken("token-1", DateTimeOffset.UtcNow.AddDays(7)));

        var firstResult = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "123456"), CancellationToken.None);
        var createdAccount = repository.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IIdentityRepository.AddBuyerAccount))
            .Select(call => (BuyerAccount)call.GetArguments()[0]!)
            .Single();

        // Second, independent OTP cycle for the SAME phone number: this time the account exists.
        var secondChallenge = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(secondChallenge);
        repository.GetBuyerAccountByPhoneNumberAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(createdAccount);
        tokenIssuer.IssueAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new IssuedAccessToken("token-2", DateTimeOffset.UtcNow.AddDays(7)));

        var secondResult = await handler.HandleAsync(new VerifyOtpCommand("+15550000000", "654321"), CancellationToken.None);

        secondResult.BuyerId.ShouldBe(firstResult.BuyerId);
        secondResult.BuyerId.ShouldBe(createdAccount.Id);
    }
}
