namespace Queue.Tests.Provisioning;

public sealed class QueueSettingsProvisioningServiceTests
{
    private readonly IQueueSettingsRepository repository = Substitute.For<IQueueSettingsRepository>();
    private readonly QueueSettingsProvisioningService service;

    public QueueSettingsProvisioningServiceTests()
    {
        service = new QueueSettingsProvisioningService(repository);
    }

    [Fact]
    public async Task ProvisionAsync_FirstTime_AddsSettingsAndSaves()
    {
        var eventId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        repository.ExistsForEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await service.ProvisionAsync(eventId, tenantId, requiresQueue: true, CancellationToken.None);

        result.ShouldBeTrue();
        repository.Received(1).Add(Arg.Is<QueueSettings>(s => s.EventId == eventId && s.TenantId == tenantId && s.Enabled));
        await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_AlreadyProvisioned_IsANoOp()
    {
        var eventId = Guid.CreateVersion7();
        repository.ExistsForEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await service.ProvisionAsync(eventId, Guid.CreateVersion7(), requiresQueue: true, CancellationToken.None);

        result.ShouldBeFalse();
        repository.DidNotReceive().Add(Arg.Any<QueueSettings>());
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_RequiresQueueFalse_ProvisionsDisabledSettings()
    {
        var eventId = Guid.CreateVersion7();
        repository.ExistsForEventAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        await service.ProvisionAsync(eventId, Guid.CreateVersion7(), requiresQueue: false, CancellationToken.None);

        repository.Received(1).Add(Arg.Is<QueueSettings>(s => !s.Enabled));
    }
}
