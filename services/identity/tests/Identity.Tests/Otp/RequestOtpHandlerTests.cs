namespace Identity.Tests.Otp;

public sealed class RequestOtpHandlerTests
{
    private readonly IIdentityRepository repository = Substitute.For<IIdentityRepository>();
    private readonly IOtpHasher hasher = Substitute.For<IOtpHasher>();
    private readonly IOtpSender sender = Substitute.For<IOtpSender>();
    private readonly RequestOtpHandler handler;

    public RequestOtpHandlerTests()
    {
        hasher.GenerateSalt().Returns("salt");
        hasher.Hash(Arg.Any<string>(), Arg.Any<string>()).Returns("hash");
        handler = new RequestOtpHandler(repository, hasher, sender);
    }

    [Fact]
    public async Task HandleAsync_InvalidPhoneNumber_ReturnsInvalidPhoneNumber_WithoutTouchingRepository()
    {
        var result = await handler.HandleAsync(new RequestOtpCommand("not-a-phone-number"), CancellationToken.None);

        result.Outcome.ShouldBe(RequestOtpOutcome.InvalidPhoneNumber);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoPriorChallenge_SendsAndReturnsSent()
    {
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns((PhoneVerification?)null);
        sender.SendAsync("+15550000000", Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.HandleAsync(new RequestOtpCommand("+15550000000"), CancellationToken.None);

        result.Outcome.ShouldBe(RequestOtpOutcome.Sent);
        result.ExpiresInSeconds.ShouldNotBeNull();
        repository.Received(1).AddPhoneVerification(Arg.Any<PhoneVerification>());
    }

    [Fact]
    public async Task HandleAsync_WithinCooldown_ReturnsRateLimited_WithSaneRetryAfter()
    {
        var recent = PhoneVerification.Issue("+15550000000", "hash", "salt", TimeSpan.FromMinutes(5));
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns(recent);

        var result = await handler.HandleAsync(new RequestOtpCommand("+15550000000"), CancellationToken.None);

        result.Outcome.ShouldBe(RequestOtpOutcome.RateLimited);
        result.RetryAfterSeconds.ShouldNotBeNull();
        result.RetryAfterSeconds!.Value.ShouldBeInRange(1, 60);
        repository.DidNotReceive().AddPhoneVerification(Arg.Any<PhoneVerification>());
    }

    [Fact]
    public async Task HandleAsync_SendFails_StillPersistsTheChallenge_SoCooldownHolds()
    {
        repository.GetLatestPhoneVerificationAsync("+15550000000", Arg.Any<CancellationToken>()).Returns((PhoneVerification?)null);
        sender.SendAsync("+15550000000", Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.HandleAsync(new RequestOtpCommand("+15550000000"), CancellationToken.None);

        result.Outcome.ShouldBe(RequestOtpOutcome.SendFailed);
        // The challenge (and thus the cooldown window) is committed BEFORE the send is attempted —
        // this assertion is the actual proof, not just that SaveChangesAsync was called once overall.
        repository.Received(1).AddPhoneVerification(Arg.Any<PhoneVerification>());
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
