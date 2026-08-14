namespace Queue.Tests.Queueing;

// The waiting room paces access but says nothing about who is asking, so a script minting fresh
// session ids takes as many places in line as it likes. Budget is charged per session *created*,
// which is the part worth pinning down: a buyer refreshing the page must never be charged, or the
// limiter would punish exactly the anxious customers a high-demand on-sale produces.
public sealed class JoinRateLimitTests
{
    private static readonly Guid EventId = Guid.CreateVersion7();

    [Fact]
    public void AResumedSession_IsNotCharged()
    {
        var resumed = new QueueStoreResult(QueueSessionStatus.Waiting, 3, WasCreated: false);

        var response = QueueStatusResponseFactory.FromStoreResult(
            EnabledSettings(), resumed, TokenIssuer(), EventId, Guid.CreateVersion7());

        response.CreatedNewSession.ShouldBeFalse();
        response.Position.ShouldBe(3);
    }

    [Fact]
    public void ANewlyEnqueuedSession_IsCharged()
    {
        var created = new QueueStoreResult(QueueSessionStatus.Waiting, 0, WasCreated: true);

        var response = QueueStatusResponseFactory.FromStoreResult(
            EnabledSettings(), created, TokenIssuer(), EventId, Guid.CreateVersion7());

        response.CreatedNewSession.ShouldBeTrue();
    }

    // An admitted session is leaving the queue, not taking a place in it.
    [Fact]
    public void AnAdmittedSession_IsNotCharged()
    {
        var admitted = new QueueStoreResult(QueueSessionStatus.Admitted, null);

        var response = QueueStatusResponseFactory.FromStoreResult(
            EnabledSettings(), admitted, TokenIssuer(), EventId, Guid.CreateVersion7());

        response.CreatedNewSession.ShouldBeFalse();
        response.Admitted.ShouldBeTrue();
    }

    // An event that never opted into queueing admits immediately without touching the store, so
    // there is no session to charge for either.
    [Fact]
    public void AnImmediateAdmit_IsNotCharged()
    {
        var response = QueueStatusResponseFactory.ImmediateAdmit(TokenIssuer(), EventId, Guid.CreateVersion7());

        response.CreatedNewSession.ShouldBeFalse();
        response.Admitted.ShouldBeTrue();
    }

    [Fact]
    public async Task JoiningAnUnqueuedEvent_NeverConsultsTheStore()
    {
        var settings = Substitute.For<IQueueSettingsRepository>();
        var store = Substitute.For<IQueueStore>();
        settings.GetByIdAsync(EventId, Arg.Any<CancellationToken>()).Returns((QueueSettings?)null);

        var response = await new JoinQueueHandler(settings, store, TokenIssuer())
            .HandleAsync(EventId, Guid.CreateVersion7(), CancellationToken.None);

        response.Admitted.ShouldBeTrue();
        response.CreatedNewSession.ShouldBeFalse();
        await store.DidNotReceive().EnqueueOrResumeAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ADenialCarriesAConcreteRetryHint()
    {
        var denied = JoinRateLimitDecision.Deny(42);

        denied.Allowed.ShouldBeFalse();
        denied.RetryAfterSeconds.ShouldBe(42);
        JoinRateLimitDecision.Allow.Allowed.ShouldBeTrue();
        JoinRateLimitDecision.Allow.RetryAfterSeconds.ShouldBeNull();
    }

    // Generous on purpose. Shared addresses are ordinary — carrier NAT, an office, the venue's own
    // wi-fi — and turning away real buyers at the moment they are trying to pay costs far more than
    // letting a script hold a handful of places.
    [Fact]
    public void TheDefaultAllowanceLeavesRoomForSharedAddresses()
    {
        var options = new QueueRateLimitOptions();

        options.MaxNewSessionsPerWindow.ShouldBeGreaterThanOrEqualTo(10);
        options.Window.ShouldBe(TimeSpan.FromMinutes(1));
    }

    private static QueueSettings EnabledSettings() =>
        QueueSettings.Create(EventId, Guid.CreateVersion7(), enabled: true);

    private static IAdmissionTokenIssuer TokenIssuer()
    {
        var issuer = Substitute.For<IAdmissionTokenIssuer>();
        issuer.Issue(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>()).Returns("token");
        return issuer;
    }
}
