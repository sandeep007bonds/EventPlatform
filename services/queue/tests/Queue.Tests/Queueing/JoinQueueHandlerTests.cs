namespace Queue.Tests.Queueing;

public sealed class JoinQueueHandlerTests
{
    private readonly IQueueSettingsRepository settingsRepository = Substitute.For<IQueueSettingsRepository>();
    private readonly IQueueStore store = Substitute.For<IQueueStore>();
    private readonly IAdmissionTokenIssuer tokenIssuer = Substitute.For<IAdmissionTokenIssuer>();
    private readonly JoinQueueHandler handler;

    public JoinQueueHandlerTests()
    {
        handler = new JoinQueueHandler(settingsRepository, store, tokenIssuer);
    }

    [Fact]
    public async Task HandleAsync_NoSettingsProvisionedYet_ImmediatelyAdmits_WithoutTouchingTheStore()
    {
        var eventId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        settingsRepository.GetByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns((QueueSettings?)null);
        tokenIssuer.Issue(eventId, sessionId, Arg.Any<TimeSpan>()).Returns("admission-token");

        var result = await handler.HandleAsync(eventId, sessionId, CancellationToken.None);

        result.Admitted.ShouldBeTrue();
        result.AdmissionToken.ShouldBe("admission-token");
        await store.DidNotReceiveWithAnyArgs().EnqueueOrResumeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SettingsDisabled_ImmediatelyAdmits_WithoutTouchingTheStore()
    {
        var eventId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var settings = QueueSettings.Create(eventId, Guid.CreateVersion7(), enabled: false);
        settingsRepository.GetByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(settings);
        tokenIssuer.Issue(eventId, sessionId, Arg.Any<TimeSpan>()).Returns("admission-token");

        var result = await handler.HandleAsync(eventId, sessionId, CancellationToken.None);

        result.Admitted.ShouldBeTrue();
        await store.DidNotReceiveWithAnyArgs().EnqueueOrResumeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SettingsEnabled_JoinsTheStore_AndReturnsWaitingWhenNotYetAdmitted()
    {
        var eventId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var settings = QueueSettings.Create(eventId, Guid.CreateVersion7(), enabled: true);
        settingsRepository.GetByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(settings);
        store.EnqueueOrResumeAsync(eventId, sessionId, Arg.Any<CancellationToken>())
            .Returns(new QueueStoreResult(QueueSessionStatus.Waiting, 3));

        var result = await handler.HandleAsync(eventId, sessionId, CancellationToken.None);

        result.Admitted.ShouldBeFalse();
        result.Position.ShouldBe(3);
        result.AdmissionToken.ShouldBeNull();
        tokenIssuer.DidNotReceiveWithAnyArgs().Issue(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task HandleAsync_SettingsEnabled_AlreadyAdmittedByTheStore_MintsAToken()
    {
        var eventId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var settings = QueueSettings.Create(eventId, Guid.CreateVersion7(), enabled: true);
        settingsRepository.GetByIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(settings);
        store.EnqueueOrResumeAsync(eventId, sessionId, Arg.Any<CancellationToken>())
            .Returns(new QueueStoreResult(QueueSessionStatus.Admitted, null));
        tokenIssuer.Issue(eventId, sessionId, Arg.Any<TimeSpan>()).Returns("admission-token");

        var result = await handler.HandleAsync(eventId, sessionId, CancellationToken.None);

        result.Admitted.ShouldBeTrue();
        result.AdmissionToken.ShouldBe("admission-token");
    }
}
